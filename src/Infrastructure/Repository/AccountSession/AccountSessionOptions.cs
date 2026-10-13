namespace SurveillanceCameras.Infrastructure.Repository.AccountSession;

/// <summary>
/// Cấu hình chung của trình duyệt đăng nhập. Trang đăng nhập và cookie nhận biết đăng nhập nằm ở từng
/// <c>AccountType</c> (BaseUrl, LoginCookieName), không cấu hình ở đây.
/// </summary>
public class AccountSessionOptions
{
    public const string SectionName = "AccountSession";

    /// <summary>Thư mục gốc chứa profile Chrome, mỗi account một thư mục con.</summary>
    public string ProfileDirectory { get; set; } = "data/browser-profiles";

    /// <summary>false = hiện cửa sổ Chrome (cần khi đăng nhập lần đầu / có captcha).</summary>
    public bool Headless { get; set; } = false;

    /// <summary>"chrome" dùng Google Chrome đã cài trên máy; để trống để dùng Chromium của Playwright.</summary>
    public string? Channel { get; set; } = "chrome";

    /// <summary>
    /// Khi app chạy dạng dịch vụ (IIS) cửa sổ Chrome nằm ở session ẩn, không ai thấy. Đặt &gt; 0 để Chrome của
    /// account N mở cổng debug <c>RemoteDebuggingPortBase + N</c> (chỉ nghe trên localhost) — đăng nhập bằng cách
    /// mở <c>chrome://inspect</c> trên chính máy đó. 0 = tắt.
    /// </summary>
    public int RemoteDebuggingPortBase { get; set; }

    /// <summary>Chu kỳ đọc lại cookie jar dù không có request nào (bắt cookie do JS đặt).</summary>
    public int PollIntervalSeconds { get; set; } = 30;

    /// <summary>Gom các response liên tiếp thành một lần kiểm tra cookie.</summary>
    public int DebounceSeconds { get; set; } = 3;
}
