using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.Commands.SyncTikTokDramas;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.ResyncAllMovies;

/// <summary>
/// Chạy nền (tác vụ hằng ngày): đồng bộ lại mọi phim đồng bộ từ drama TikTok của mọi người dùng — cập nhật lượt xem,
/// thông tin và thêm tập mới. Không tạo phim mới. Lỗi của từng kênh/phim được ghi lại, không dừng cả lượt.
/// </summary>
/// <param name="DelayBetweenDramas">Nghỉ giữa hai drama để không dồn dập request tới TikTok.</param>
public record ResyncAllMoviesCommand(TimeSpan DelayBetweenDramas) : IRequest<ResyncAllMoviesResult>;

public record ResyncAllMoviesResult(int Movies, int Updated, int EpisodesAdded, int Failed);

public class ResyncAllMoviesCommandHandler : IRequestHandler<ResyncAllMoviesCommand, ResyncAllMoviesResult>
{
    private readonly IApplicationDbContext _context;
    private readonly TikTokDramaSyncer _syncer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ResyncAllMoviesCommandHandler> _logger;

    public ResyncAllMoviesCommandHandler(IApplicationDbContext context, TikTokDramaSyncer syncer, TimeProvider timeProvider,
        ILogger<ResyncAllMoviesCommandHandler> logger)
    {
        _context = context;
        _syncer = syncer;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ResyncAllMoviesResult> Handle(ResyncAllMoviesCommand request, CancellationToken cancellationToken)
    {
        var movies = await _context.Works
            .AsNoTracking()
            .Where(x => x.Type == WorkType.Movie && x.UserProfileId != null && x.ExternalId != null)
            .OrderBy(x => x.Id)
            .Select(x => new { x.CreatedBy, UserProfileId = x.UserProfileId!.Value, x.ExternalId })
            .ToListAsync(cancellationToken);

        int updated = 0, episodesAdded = 0, failed = 0;
        var first = true;

        foreach (var movie in movies)
        {
            if (!first) await Task.Delay(request.DelayBetweenDramas, _timeProvider, cancellationToken);
            first = false;

            try
            {
                // Từng phim một: lỗi của phim này không kéo theo phim khác.
                var result = await _syncer.SyncAsync(movie.CreatedBy, movie.UserProfileId, [movie.ExternalId!], cancellationToken);
                updated += result.Updated;
                episodesAdded += result.EpisodesAdded;
                if (result.Errors.Count > 0) failed++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Kênh đã bị xoá khỏi danh sách/không còn là kênh TikTok…
                failed++;
                _logger.LogWarning("Tự đồng bộ phim (kênh {UserProfileId}, drama {DramaId}) lỗi: {Error}",
                    movie.UserProfileId, movie.ExternalId, ex.Message);
            }

            // Mỗi phim một DbContext sạch: không giữ hàng nghìn tập trong bộ nhớ theo dõi.
            _context.ChangeTracker.Clear();
        }

        _logger.LogInformation("Tự đồng bộ phim xong: {Movies} phim, cập nhật {Updated}, thêm {Episodes} tập, lỗi {Failed}",
            movies.Count, updated, episodesAdded, failed);
        return new ResyncAllMoviesResult(movies.Count, updated, episodesAdded, failed);
    }
}
