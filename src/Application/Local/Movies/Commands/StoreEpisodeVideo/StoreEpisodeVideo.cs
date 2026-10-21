using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;

namespace SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideo;

/// <summary>
/// Chạy nền (từ Kafka): tải video của một tập từ TikTok và lưu lên kho R2 của chủ phim — chỉ khi còn chỗ trống.
/// Lỗi của tập được ghi vào tập; chỉ <c>TikTokBlockedException</c> (TikTok chặn, chưa đăng nhập) được ném ra để bên gọi nghỉ.
/// </summary>
public record StoreEpisodeVideoCommand(int EpisodeId) : IRequest;

public class StoreEpisodeVideoCommandHandler : IRequestHandler<StoreEpisodeVideoCommand>
{
    private readonly EpisodeVideoStorer _storer;

    public StoreEpisodeVideoCommandHandler(EpisodeVideoStorer storer) => _storer = storer;

    public Task Handle(StoreEpisodeVideoCommand request, CancellationToken cancellationToken)
        => _storer.StoreQueuedAsync(request.EpisodeId, cancellationToken);
}
