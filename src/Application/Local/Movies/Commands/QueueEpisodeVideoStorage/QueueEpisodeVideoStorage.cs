using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;

namespace SurveillanceCameras.Application.Local.Movies.Commands.QueueEpisodeVideoStorage;

/// <summary>
/// Chạy định kỳ: tìm các tập phim chưa có video trên R2 và đưa vào hàng đợi Kafka để tải lên — chỉ với người dùng còn đủ
/// chỗ trống ở kho, và không quá số tập kho còn chứa được. Tập đã lỗi hết số lần thử được mở lại thử một lần sau một
/// khoảng (<c>ExhaustedRetryHours</c>).
/// </summary>
/// <returns>Số tập được đưa vào hàng đợi.</returns>
public record QueueEpisodeVideoStorageCommand : IRequest<int>;

public class QueueEpisodeVideoStorageCommandHandler : IRequestHandler<QueueEpisodeVideoStorageCommand, int>
{
    private readonly EpisodeVideoStorer _storer;
    private readonly TimeProvider _timeProvider;

    public QueueEpisodeVideoStorageCommandHandler(EpisodeVideoStorer storer, TimeProvider timeProvider)
    {
        _storer = storer;
        _timeProvider = timeProvider;
    }

    public Task<int> Handle(QueueEpisodeVideoStorageCommand request, CancellationToken cancellationToken)
        => _storer.QueuePendingAsync(_timeProvider.GetUtcNow(), cancellationToken);
}
