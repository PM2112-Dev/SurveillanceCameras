namespace SurveillanceCameras.Domain.Enums;

public enum AccountSessionState
{
    /// <summary>Không có trình duyệt nào đang chạy cho account này.</summary>
    Stopped,

    /// <summary>Trình duyệt đang mở, chờ người dùng đăng nhập.</summary>
    WaitingForLogin,

    /// <summary>Đã đăng nhập, cookie đang được theo dõi và cập nhật.</summary>
    Active,

    /// <summary>Đã từng đăng nhập nhưng phiên bị TikTok đăng xuất, cần đăng nhập lại.</summary>
    LoggedOut,
}
