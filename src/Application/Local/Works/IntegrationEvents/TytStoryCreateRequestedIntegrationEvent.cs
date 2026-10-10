namespace SurveillanceCameras.Application.Local.Works.IntegrationEvents;

/// <summary>Đưa truyện lên TYT bằng tài khoản của truyện (<c>Story.AccountId</c>): tạo nếu chưa có, rồi cập nhật thông tin.</summary>
public sealed class TytStoryCreateRequestedIntegrationEvent : IntegrationEvent
{
    public required int StoryId { get; init; }
}
