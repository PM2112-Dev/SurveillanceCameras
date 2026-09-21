namespace SurveillanceCameras.Application.Common.Exceptions;

/// <summary>TikTok trả về lỗi, phản hồi rỗng hoặc sai định dạng (cookie hết hạn, bị chặn…).</summary>
public class TikTokApiException : Exception
{
    public TikTokApiException(string message) : base(message) { }

    public TikTokApiException(string message, Exception innerException) : base(message, innerException) { }
}
