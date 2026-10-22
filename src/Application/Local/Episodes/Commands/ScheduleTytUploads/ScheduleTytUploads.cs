using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Episodes.Commands.ScheduleTytUploads;

/// <summary>
/// Chạy nền (mỗi phút): với mọi truyện đã có trên TYT và đặt lịch đăng, đưa lô chương kế tiếp vào hàng đợi khi tới lượt —
/// ngay khi xử lý xong, mỗi N giờ hoặc mỗi ngày vào giờ cố định (xem <see cref="TytUploadSchedule"/>).
/// </summary>
/// <returns>Số chương được đưa vào hàng đợi.</returns>
public record ScheduleTytUploadsCommand(TimeZoneInfo TimeZone, int ImmediateBatchSize = 20) : IRequest<int>;

public class ScheduleTytUploadsCommandHandler : IRequestHandler<ScheduleTytUploadsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ScheduleTytUploadsCommandHandler> _logger;

    public ScheduleTytUploadsCommandHandler(IApplicationDbContext context, TimeProvider timeProvider,
        ILogger<ScheduleTytUploadsCommandHandler> logger)
    {
        _context = context;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> Handle(ScheduleTytUploadsCommand request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        var works = await _context.Works
            .Where(x => x.Type == WorkType.Story && x.TytUploadMode != TytUploadMode.Manual && x.TytStage == TytStage.Approved
                        && x.TytStoryId != null && x.TytStoryId != "" && x.AccountId != null)
            .ToListAsync(cancellationToken);

        var queued = 0;
        foreach (var work in works)
        {
            var lastUploaded = await _context.Episodes.Where(x => x.WorkId == work.Id)
                .MaxAsync(x => x.TytUploadedAt, cancellationToken);
            var lastQueued = await _context.Episodes.Where(x => x.WorkId == work.Id)
                .MaxAsync(x => x.QueuedAt, cancellationToken);
            var last = lastUploaded is null ? lastQueued : lastQueued is null ? lastUploaded : DateTimeOffset.Compare(lastUploaded.Value, lastQueued.Value) >= 0 ? lastUploaded : lastQueued;

            if (!TytUploadSchedule.IsDue(work.TytUploadMode, work.TytUploadEveryHours, work.TytUploadDailyTime, last, now,
                    request.TimeZone))
                continue;

            var batch = work.TytUploadMode == TytUploadMode.Immediate
                ? request.ImmediateBatchSize
                : Math.Max(1, work.TytUploadBatchSize);

            // Hạn mức chương mỗi ngày: chỉ đăng phần còn lại của hôm nay.
            if (work.TytUploadDailyLimit > 0)
            {
                var localNow = TimeZoneInfo.ConvertTime(now, request.TimeZone);
                var dayStart = new DateTimeOffset(localNow.Date, localNow.Offset);
                batch = Math.Min(batch, work.TytUploadDailyLimit - await TytUploadQueue.CountSinceAsync(_context, work.Id, dayStart, cancellationToken));
                if (batch <= 0) continue;
            }

            queued += await TytUploadQueue.QueueNextAsync(_context, work, batch, now, cancellationToken);
        }

        if (queued > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã đưa {Count} chương vào hàng đợi đăng lên TYT", queued);
        }

        return queued;
    }
}
