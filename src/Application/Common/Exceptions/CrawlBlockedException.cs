namespace SurveillanceCameras.Application.Common.Exceptions;

/// <summary>
/// Trang nguồn đang chặn/giới hạn request (429/403) nên hệ thống tạm nghỉ không gọi nó tới <see cref="RetryAt"/>.
/// Khác lỗi crawl thường: việc đang làm chưa hỏng, chỉ cần chờ rồi làm lại — nơi bắt lỗi không được đánh dấu thất bại mà phải
/// để lỗi này đi tiếp tới consumer (chờ rồi thử lại).
/// </summary>
public class CrawlBlockedException : CrawlException
{
    public CrawlBlockedException(string message, DateTimeOffset retryAt) : base(message)
    {
        RetryAt = retryAt;
    }

    public DateTimeOffset RetryAt { get; }
}
