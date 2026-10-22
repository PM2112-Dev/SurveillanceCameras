using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes;
using SurveillanceCameras.Application.Repository.Tyt;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Tyt.Commands.AdvanceTytPublish;

/// <summary>
/// Chạy định kỳ: đẩy các truyện đã yêu cầu xuất bản (<see cref="TytStage.PublishRequested"/>) tiến lên. Cần đăng đủ vài chương
/// đầu (<see cref="TytPolicyOptions.FirstChaptersToPublish"/>) trước: chương nào xử lý xong thì xếp hàng đăng; khi các chương
/// đầu đã lên TYT thì gửi yêu cầu công khai và chuyển sang chờ duyệt.
/// </summary>
/// <returns>Số truyện chuyển sang chờ duyệt trong lượt này.</returns>
public record AdvanceTytPublishCommand : IRequest<int>;

public class AdvanceTytPublishCommandHandler : IRequestHandler<AdvanceTytPublishCommand, int>
{
    private const int MaxNoteLength = 2000;

    private readonly IApplicationDbContext _context;
    private readonly ITytRepository _tyt;
    private readonly TimeProvider _timeProvider;
    private readonly TytPolicyOptions _policy;
    private readonly ILogger<AdvanceTytPublishCommandHandler> _logger;

    public AdvanceTytPublishCommandHandler(IApplicationDbContext context, ITytRepository tyt, TimeProvider timeProvider,
        IOptions<TytPolicyOptions> policy, ILogger<AdvanceTytPublishCommandHandler> logger)
    {
        _context = context;
        _tyt = tyt;
        _timeProvider = timeProvider;
        _policy = policy.Value;
        _logger = logger;
    }

    public async Task<int> Handle(AdvanceTytPublishCommand request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var retryBefore = now - TimeSpan.FromMinutes(Math.Max(1, _policy.RetryMinutes));

        var works = await _context.Works
            .Where(x => x.Type == WorkType.Story && x.TytStage == TytStage.PublishRequested)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var moved = 0;
        foreach (var work in works)
        {
            if (!TytUploadQueue.CanUpload(work))
            {
                work.TytStageNote = "Truyện chưa có trên TYT hoặc chưa chọn tài khoản đăng.";
                continue;
            }

            var first = await _context.Episodes
                .AsNoTracking()
                .Where(x => x.WorkId == work.Id)
                .OrderBy(x => x.EpisodeNumber).ThenBy(x => x.Id)
                .Take(Math.Max(1, _policy.FirstChaptersToPublish))
                .Select(x => new { x.IsUploaded, Ready = x.Status == EpisodeStatus.Edited && x.ContentEdit != null && x.ContentEdit != "", x.TytUploadError })
                .ToListAsync(cancellationToken);

            if (first.Count == 0)
            {
                work.TytStageNote = "Truyện chưa có chương nào để đăng.";
                continue;
            }

            if (first.Any(x => !x.IsUploaded))
            {
                var ready = first.Count(x => x.IsUploaded || x.Ready);
                var failed = first.FirstOrDefault(x => x.TytUploadError is not null)?.TytUploadError;
                work.TytStageNote = failed is not null
                    ? $"Đăng chương đầu lỗi: {failed}"
                    : $"Đang đăng {first.Count} chương đầu: {first.Count(x => x.IsUploaded)}/{first.Count} đã lên TYT, {ready}/{first.Count} đã xử lý xong.";
                // Chương đầu chưa xử lý xong thì chờ; xong rồi thì xếp hàng đăng (chương đang trong hàng đợi không bị xếp lại).
                await TytUploadQueue.QueueNextAsync(_context, work, first.Count, now, cancellationToken);
                continue;
            }

            // Chương đầu đã lên TYT: gửi yêu cầu công khai (giãn cách khi TYT báo lỗi để không gửi dồn).
            if (work.TytRetryQueuedAt is { } last && last > retryBefore) continue;
            work.TytRetryQueuedAt = now;
            try
            {
                var context = await TytAccounts.ContextAsync(_context, work.AccountId, work.CreatedBy, cancellationToken);
                var message = await _tyt.RequestPublishAsync(context, work.TytStoryId!, cancellationToken);

                work.TytStage = TytStage.PendingApproval;
                work.TytPublishRequestedAt = now;
                work.TytStageNote = Truncate(string.IsNullOrWhiteSpace(message) ? "Đã gửi yêu cầu công khai, chờ TYT duyệt." : $"Đã gửi yêu cầu công khai, chờ TYT duyệt. TYT: {message}");
                work.TytError = null;
                moved++;
            }
            catch (TytException ex)
            {
                _logger.LogWarning("Story {StoryId}: gửi yêu cầu công khai lỗi: {Error}", work.Id, ex.Message);
                work.TytStageNote = Truncate($"Gửi yêu cầu công khai lỗi, sẽ thử lại: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return moved;
    }

    private static string Truncate(string text) => text.Length <= MaxNoteLength ? text : text[..MaxNoteLength];
}
