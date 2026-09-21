namespace SurveillanceCameras.Infrastructure.Service.TikTokApi;

public class TikTokApiOptions
{
    public const string SectionName = "TikTokApi";

    /// <summary>Địa chỉ gốc của TikTok web. Cấu hình được, không hard-code.</summary>
    public string BaseUrl { get; set; } = "https://www.tiktok.com";

    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/153.0.0.0 Safari/537.36";

    /// <summary>Ngôn ngữ gửi lên TikTok (tham số <c>language</c>, <c>app_language</c>).</summary>
    public string Language { get; set; } = "vi-VN";

    /// <summary>Vùng gửi lên TikTok (tham số <c>region</c>, <c>priority_region</c>).</summary>
    public string Region { get; set; } = "VN";

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>"chrome" dùng Google Chrome đã cài trên máy; để trống để dùng Chromium của Playwright.</summary>
    public string? BrowserChannel { get; set; } = "chrome";

    /// <summary>Số trang Chrome headless được mở đồng thời khi lấy profile.</summary>
    public int MaxConcurrentBrowserPages { get; set; } = 2;
}
