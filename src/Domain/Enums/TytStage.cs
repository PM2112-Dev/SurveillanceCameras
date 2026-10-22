namespace SurveillanceCameras.Domain.Enums;

/// <summary>Truyện đang ở bước nào của quy trình đưa lên TYT. Giá trị số được lưu trong DB.</summary>
public enum TytStage
{
    /// <summary>Chưa xuất bản (truyện chưa có trên TYT, hoặc đã tạo nhưng chưa yêu cầu xuất bản).</summary>
    None = 0,

    /// <summary>Tài khoản TYT đã đủ số truyện đang đăng cùng lúc: chờ có chỗ trống (truyện khác hoàn thành) rồi mới tạo trên TYT.</summary>
    WaitingSlot = 1,

    /// <summary>Đã yêu cầu xuất bản: đang đăng vài chương đầu, xong sẽ gửi yêu cầu công khai.</summary>
    PublishRequested = 2,

    /// <summary>Đã gửi yêu cầu công khai, chờ TYT duyệt.</summary>
    PendingApproval = 3,

    /// <summary>TYT đã duyệt (công khai): bắt đầu đăng các chương còn lại theo lịch.</summary>
    Approved = 4
}
