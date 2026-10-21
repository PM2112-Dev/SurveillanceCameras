namespace SurveillanceCameras.Application.Local.Movies.IntegrationEvents;

/// <summary>Đồng bộ lại một drama TikTok đã có thành phim: cập nhật lượt xem, thông tin và thêm tập mới.</summary>
public sealed class MovieSyncRequestedIntegrationEvent : IntegrationEvent
{
    /// <summary>Chủ phim (tác vụ nền không có người dùng đăng nhập).</summary>
    public string? OwnerId { get; init; }

    public required int UserProfileId { get; init; }

    public required string DramaId { get; init; }
}
