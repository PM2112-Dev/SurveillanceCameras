namespace SurveillanceCameras.Domain.Entities;

/// <summary>Trang web truyện dùng làm nguồn crawl.</summary>
public class WebSource : BaseAuditableEntity
{
    public string? BaseUrl { get; set; }

    /// <summary>Cách crawl của trang (chọn repository trong ICrawlStoryRepositoryFactory).</summary>
    public WebSourceType? Type { get; set; }

    /// <summary>Trang cần cookie đăng nhập mới gọi được API; khi bật thì bắt buộc có <see cref="AccountId"/>.</summary>
    public bool IsCookie { get; set; }

    /// <summary>Tài khoản cung cấp cookie khi crawl.</summary>
    public int? AccountId { get; set; }

    public Account? Account { get; set; }

    /// <summary>
    /// Truyện từ nguồn này cần AI biên tập lại (tên, giới thiệu, nội dung chương). Tắt khi nguồn đã là văn tiếng Việt
    /// dùng được: truyện và chương giữ nguyên nội dung gốc, không tốn lượt gọi AI.
    /// </summary>
    public bool IsNeedEdit { get; set; } = true;

    /// <summary>
    /// Tài khoản TYT đăng truyện: có thì truyện tạo từ nguồn này được tự động tạo trên TYT bằng tài khoản này
    /// (gán vào <c>Story.AccountId</c>). Để trống thì không đăng.
    /// </summary>
    public int? TytAccountId { get; set; }

    public Account? TytAccount { get; set; }

    /// <summary>
    /// Cấu hình crawl dạng JSON (xem <c>CrawlConfig</c> ở Application). Có cấu hình thì không cần repository
    /// viết bằng code cho nguồn này.
    /// </summary>
    public string? CrawlConfig { get; set; }

    public ICollection<StorySource>? StorySources { get; set; }
}
