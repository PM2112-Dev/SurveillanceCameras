namespace SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;

/// <summary>Đăng một chương đã xử lý xong lên TYT bằng tài khoản của truyện.</summary>
public sealed class ChapterTytUploadRequestedIntegrationEvent : IntegrationEvent
{
    public required int ChapterId { get; init; }

    /// <summary>Cập nhật lại chương đã đăng (nội dung vừa sửa) thay vì bỏ qua.</summary>
    public bool Force { get; init; }
}
