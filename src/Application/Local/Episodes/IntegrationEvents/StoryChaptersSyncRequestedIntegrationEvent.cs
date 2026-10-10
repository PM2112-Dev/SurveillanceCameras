namespace SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;

/// <summary>
/// Đồng bộ MỘT trang mục lục chương của truyện từ trang nguồn. Xử lý xong trang này thì phát tiếp sự kiện cho trang
/// sau — mục lục hàng nghìn chương được chia thành nhiều message ngắn thay vì một tác vụ dài.
/// </summary>
public sealed class StoryChaptersSyncRequestedIntegrationEvent : IntegrationEvent
{
    public required int StoryId { get; init; }

    /// <summary>Trang mục lục (bắt đầu từ 1).</summary>
    public int Page { get; init; } = 1;
}
