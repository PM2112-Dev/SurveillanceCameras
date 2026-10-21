namespace SurveillanceCameras.Application.Local.Movies.IntegrationEvents;

/// <summary>Tải video của một tập phim từ TikTok rồi lưu lên kho R2 của chủ phim.</summary>
public sealed class EpisodeVideoStoreRequestedIntegrationEvent : IntegrationEvent
{
    public required int EpisodeId { get; init; }
}
