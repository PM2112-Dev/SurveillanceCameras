namespace SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;

/// <summary>Tải nội dung gốc của một chương từ trang nguồn. Chỉ mang Id — nội dung chương không đi qua Kafka.</summary>
public sealed class ChapterFetchRequestedIntegrationEvent : IntegrationEvent
{
    public required int ChapterId { get; init; }
}
