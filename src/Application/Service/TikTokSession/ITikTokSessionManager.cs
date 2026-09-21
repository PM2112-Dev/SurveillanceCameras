namespace SurveillanceCameras.Application.Service.TikTokSession;

public enum TikTokSessionState
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

public record TikTokSessionStatus(
    int AccountId,
    TikTokSessionState State,
    DateTimeOffset? CookieUpdatedAt = null);

/// <summary>
/// Giữ một trình duyệt Chrome luôn mở trên TikTok cho từng account, theo dõi cookie thay đổi
/// (không cần tải lại trang) và ghi cookie mới nhất vào <c>Account.Cookie</c>.
/// </summary>
public interface ITikTokSessionManager
{
    /// <summary>Mở trình duyệt (profile riêng theo account). Nếu đã chạy thì bỏ qua.</summary>
    Task StartAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>Đóng trình duyệt. Profile được giữ lại nên lần sau vẫn còn đăng nhập.</summary>
    Task StopAsync(int accountId);

    TikTokSessionStatus GetStatus(int accountId);
}
