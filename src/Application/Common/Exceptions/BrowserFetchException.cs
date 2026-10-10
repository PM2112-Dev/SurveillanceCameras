namespace SurveillanceCameras.Application.Common.Exceptions;

/// <summary>Không gửi được request qua phiên Chrome của tài khoản (trình duyệt đóng, hết giờ, URL không hợp lệ…).</summary>
public class BrowserFetchException : Exception
{
    public BrowserFetchException(string message) : base(message) { }

    public BrowserFetchException(string message, Exception innerException) : base(message, innerException) { }
}
