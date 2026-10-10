namespace SurveillanceCameras.Application.Common.Exceptions;

/// <summary>Không lấy được dữ liệu từ trang nguồn (lỗi mạng, bị chặn, cấu hình crawl sai…).</summary>
public class CrawlException : Exception
{
    public CrawlException(string message) : base(message) { }

    public CrawlException(string message, Exception innerException) : base(message, innerException) { }
}
