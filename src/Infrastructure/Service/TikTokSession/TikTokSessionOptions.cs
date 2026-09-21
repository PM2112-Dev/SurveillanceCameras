namespace SurveillanceCameras.Infrastructure.Service.TikTokSession;

public class TikTokSessionOptions
{
    public const string SectionName = "TikTokSession";

    /// <summary>Trang mở khi bắt đầu phiên. Cấu hình được, không hard-code.</summary>
    public string LoginUrl { get; set; } = "https://www.tiktok.com/login";

    /// <summary>Chỉ lấy cookie có domain kết thúc bằng giá trị này.</summary>
    public string CookieDomain { get; set; } = "tiktok.com";

    /// <summary>Cookie dùng để nhận biết đã đăng nhập.</summary>
    public string SessionCookieName { get; set; } = "sessionid";

    /// <summary>Thư mục gốc chứa profile Chrome, mỗi account một thư mục con.</summary>
    public string ProfileDirectory { get; set; } = "data/browser-profiles/tiktok";

    /// <summary>false = hiện cửa sổ Chrome (cần khi đăng nhập lần đầu / có captcha).</summary>
    public bool Headless { get; set; } = false;

    /// <summary>"chrome" dùng Google Chrome đã cài trên máy; để trống để dùng Chromium của Playwright.</summary>
    public string? Channel { get; set; } = "chrome";

    /// <summary>Chu kỳ đọc lại cookie jar dù không có request nào (bắt cookie do JS đặt).</summary>
    public int PollIntervalSeconds { get; set; } = 30;

    /// <summary>Gom các response liên tiếp thành một lần kiểm tra cookie.</summary>
    public int DebounceSeconds { get; set; } = 3;
}
