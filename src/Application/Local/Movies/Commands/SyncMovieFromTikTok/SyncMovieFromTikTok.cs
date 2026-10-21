using SurveillanceCameras.Application.Local.Movies.Commands.SyncTikTokDramas;

namespace SurveillanceCameras.Application.Local.Movies.Commands.SyncMovieFromTikTok;

/// <summary>Chạy nền (từ Kafka): đồng bộ lại một drama đã có thành phim. Không tạo phim mới.</summary>
public record SyncMovieFromTikTokCommand(string? OwnerId, int UserProfileId, string DramaId) : IRequest<SyncTikTokDramasResult>;

public class SyncMovieFromTikTokCommandHandler : IRequestHandler<SyncMovieFromTikTokCommand, SyncTikTokDramasResult>
{
    private readonly TikTokDramaSyncer _syncer;

    public SyncMovieFromTikTokCommandHandler(TikTokDramaSyncer syncer) => _syncer = syncer;

    public Task<SyncTikTokDramasResult> Handle(SyncMovieFromTikTokCommand request, CancellationToken cancellationToken)
        => _syncer.SyncAsync(request.OwnerId, request.UserProfileId, [request.DramaId], cancellationToken);
}
