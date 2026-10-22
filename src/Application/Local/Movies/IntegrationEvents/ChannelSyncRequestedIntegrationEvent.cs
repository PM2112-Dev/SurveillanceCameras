namespace SurveillanceCameras.Application.Local.Movies.IntegrationEvents;

/// <summary>Kiểm tra kênh TikTok đã lưu: drama (phim) mới và tập mới được đồng bộ về, drama đã xoá thì bỏ qua.</summary>
public sealed class ChannelSyncRequestedIntegrationEvent : IntegrationEvent
{
    public string? OwnerId { get; init; }

    public required int UserProfileId { get; init; }
}
