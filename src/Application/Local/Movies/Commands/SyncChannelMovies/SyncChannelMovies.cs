using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.IntegrationEvents;
using SurveillanceCameras.Application.Repository.TikTok;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.SyncChannelMovies;

/// <summary>
/// Chạy nền (từ Kafka): đọc toàn bộ drama của một kênh TikTok đã lưu rồi đưa từng drama vào hàng đợi đồng bộ
/// (<see cref="MovieSyncRequestedIntegrationEvent"/>) — drama chưa có thì tạo phim mới, drama đã có thì cập nhật và thêm tập
/// mới. Drama người dùng đã xoá (<c>UserProfile.ExcludedDramaIds</c>) bị bỏ qua. Lỗi (TikTok chặn, kênh đổi) được ghi vào
/// kênh để người dùng thấy.
/// </summary>
/// <param name="PageDelay">Nghỉ giữa hai trang danh sách drama để không dồn request tới TikTok.</param>
public record SyncChannelMoviesCommand(string? OwnerId, int UserProfileId, TimeSpan PageDelay) : IRequest<SyncChannelMoviesResult>;

/// <param name="Dramas">Số drama trên kênh.</param>
/// <param name="Queued">Số drama được đưa vào hàng đợi đồng bộ.</param>
/// <param name="New">Trong đó, số drama chưa có phim (sẽ được tạo mới).</param>
public record SyncChannelMoviesResult(int Dramas, int Queued, int New);

public class SyncChannelMoviesCommandHandler : IRequestHandler<SyncChannelMoviesCommand, SyncChannelMoviesResult>
{
    private const int PageSize = 30;

    /// <summary>Chặn vòng lặp nếu TikTok trả hasMore mãi (kênh có vài trăm drama).</summary>
    private const int MaxPages = 50;

    private const int MaxErrorLength = 2000;

    private readonly IApplicationDbContext _context;
    private readonly ITikTokRepository _tikTok;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SyncChannelMoviesCommandHandler> _logger;

    public SyncChannelMoviesCommandHandler(IApplicationDbContext context, ITikTokRepository tikTok, TimeProvider timeProvider,
        ILogger<SyncChannelMoviesCommandHandler> logger)
    {
        _context = context;
        _tikTok = tikTok;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<SyncChannelMoviesResult> Handle(SyncChannelMoviesCommand request, CancellationToken cancellationToken)
    {
        var channel = await _context.UserProfiles
            .Include(x => x.UserProfileType)
            .FirstOrDefaultAsync(x => x.Id == request.UserProfileId && x.CreatedBy == request.OwnerId, cancellationToken);
        if (channel is null) return new SyncChannelMoviesResult(0, 0, 0);

        try
        {
            if (string.IsNullOrWhiteSpace(channel.SecUid))
                throw new TikTokApiException("Kênh chưa có secUid — tra cứu lại kênh để lấy drama.");

            var dramaIds = new List<string>();
            var seen = new HashSet<string>();
            var cursor = "0";
            for (var page = 0; page < MaxPages; page++)
            {
                if (page > 0 && request.PageDelay > TimeSpan.Zero) await Task.Delay(request.PageDelay, _timeProvider, cancellationToken);

                var result = await _tikTok.FetchDramaList(null, channel.SecUid, PageSize, cursor, cancellationToken);
                var added = result.DramaList.Select(x => x.DramaId).OfType<string>().Where(seen.Add).ToList();
                dramaIds.AddRange(added);

                if (!result.HasMore || added.Count == 0 || string.IsNullOrEmpty(result.Cursor) || result.Cursor == cursor) break;
                cursor = result.Cursor;
            }

            var excluded = channel.ExcludedDramaIds.ToHashSet();
            var wanted = dramaIds.Where(x => !excluded.Contains(x)).ToList();
            var existing = (await _context.Works
                    .AsNoTracking()
                    .Where(x => x.CreatedBy == request.OwnerId && x.Type == WorkType.Movie && x.UserProfileId == channel.Id
                                && x.ExternalId != null && wanted.Contains(x.ExternalId))
                    .Select(x => x.ExternalId!)
                    .ToListAsync(cancellationToken))
                .ToHashSet();

            // Sự kiện gắn vào bản ghi kênh (OutboxInterceptor lấy sự kiện từ mọi entity đang được theo dõi).
            foreach (var dramaId in wanted)
            {
                channel.AddIntegrationEvent(new MovieSyncRequestedIntegrationEvent
                {
                    OwnerId = request.OwnerId, UserProfileId = channel.Id, DramaId = dramaId
                }, cancellationToken);
            }

            channel.MoviesSyncedAt = _timeProvider.GetUtcNow();
            channel.MoviesSyncError = null;
            await _context.SaveChangesAsync(cancellationToken);

            var created = wanted.Count(x => !existing.Contains(x));
            _logger.LogInformation("Kênh {ChannelId}: {Dramas} drama, đưa {Queued} vào hàng đợi ({New} phim mới)",
                channel.Id, dramaIds.Count, wanted.Count, created);
            return new SyncChannelMoviesResult(dramaIds.Count, wanted.Count, created);
        }
        catch (TikTokApiException ex)
        {
            // Ghi lỗi để người dùng thấy; lượt định kỳ sau tự thử lại (TikTok chặn thì thường hết sau một lúc).
            _logger.LogWarning("Kênh {ChannelId}: kiểm tra phim mới lỗi: {Error}", channel.Id, ex.Message);
            channel.MoviesSyncError = ex.Message.Length <= MaxErrorLength ? ex.Message : ex.Message[..MaxErrorLength];
            await _context.SaveChangesAsync(cancellationToken);
            return new SyncChannelMoviesResult(0, 0, 0);
        }
    }
}
