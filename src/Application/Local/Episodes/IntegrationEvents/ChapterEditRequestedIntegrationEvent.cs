namespace SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;

/// <summary>Dùng AI biên tập nội dung một chương (prompt loại <c>edit-chapter-content</c>).</summary>
public sealed class ChapterEditRequestedIntegrationEvent : IntegrationEvent
{
    public required int ChapterId { get; init; }

    /// <summary>Biên tập lại cả chương đã biên tập (người dùng yêu cầu); mặc định bỏ qua chương đã xong.</summary>
    public bool Force { get; init; }
}
