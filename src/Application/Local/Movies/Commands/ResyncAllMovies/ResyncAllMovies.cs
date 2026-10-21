using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.IntegrationEvents;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.ResyncAllMovies;

/// <summary>
/// Chạy nền (tác vụ hằng ngày): đưa mọi phim đồng bộ từ drama TikTok của mọi người dùng vào hàng đợi Kafka để đồng bộ lại —
/// cập nhật lượt xem, thông tin và thêm tập mới. Mỗi phim là một sự kiện nên lỗi của phim này không kéo theo phim khác,
/// lỗi tạm thời được Kafka tự thử lại, và nhịp gọi TikTok do consumer giữ. Không tạo phim mới.
/// </summary>
public record ResyncAllMoviesCommand : IRequest<ResyncAllMoviesResult>;

/// <param name="Movies">Số phim đồng bộ từ TikTok.</param>
/// <param name="Queued">Số phim đã đưa vào hàng đợi.</param>
public record ResyncAllMoviesResult(int Movies, int Queued);

public class ResyncAllMoviesCommandHandler : IRequestHandler<ResyncAllMoviesCommand, ResyncAllMoviesResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ResyncAllMoviesCommandHandler> _logger;

    public ResyncAllMoviesCommandHandler(IApplicationDbContext context, ILogger<ResyncAllMoviesCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResyncAllMoviesResult> Handle(ResyncAllMoviesCommand request, CancellationToken cancellationToken)
    {
        // Sự kiện gắn vào bản ghi phim (OutboxInterceptor lấy sự kiện từ mọi entity đang được theo dõi).
        var movies = await _context.Works
            .Where(x => x.Type == WorkType.Movie && x.UserProfileId != null && x.ExternalId != null)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var movie in movies)
        {
            movie.AddIntegrationEvent(new MovieSyncRequestedIntegrationEvent
            {
                OwnerId = movie.CreatedBy,
                UserProfileId = movie.UserProfileId!.Value,
                DramaId = movie.ExternalId!
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tự đồng bộ phim: đã đưa {Movies} phim vào hàng đợi", movies.Count);
        return new ResyncAllMoviesResult(movies.Count, movies.Count);
    }
}
