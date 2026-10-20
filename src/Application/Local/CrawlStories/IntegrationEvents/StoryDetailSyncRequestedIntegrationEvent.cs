using SurveillanceCameras.Domain.Common;

namespace SurveillanceCameras.Application.Local.CrawlStories.IntegrationEvents;

/// <summary>
/// Yêu cầu tải trang chi tiết của một truyện đã chọn khi đồng bộ từ nguồn web. Mỗi truyện một message, consumer xử lý
/// tuần tự nên các lần gọi tới trang nguồn cách nhau theo cấu hình giãn cách (<c>requestDelayMs</c>).
/// </summary>
public sealed class StoryDetailSyncRequestedIntegrationEvent : IntegrationEvent
{
    public required int StorySourceId { get; init; }

    /// <summary>Nguồn truyện vừa được thêm: sau khi có chi tiết thì phát sự kiện tạo truyện (AI biên tập).</summary>
    public bool IsNew { get; init; }
}
