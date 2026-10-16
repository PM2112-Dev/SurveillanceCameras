namespace SurveillanceCameras.Application.Common.Exceptions;

/// <summary>
/// Yêu cầu từ app bị từ chối vì một quy tắc nghiệp vụ, với mã HTTP và lời nhắn hiện được cho người xem
/// (vd. hết lượt xem quảng cáo trong ngày → 429, gói đã gắn với tài khoản khác → 409).
/// </summary>
public class AppRequestException : Exception
{
    public AppRequestException(int statusCode, string message, string? code = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public int StatusCode { get; }

    /// <summary>Mã lỗi cố định để app phân biệt (vd. <c>ad_reward_pending</c>).</summary>
    public string? Code { get; }

    public static AppRequestException AdRewardPending() => new(409,
        "Đang chờ xác nhận lượt xem quảng cáo, thử lại sau vài giây.", "ad_reward_pending");

    public static AppRequestException AdLimitReached(int limit) => new(429,
        $"Bạn đã mở {limit} tập bằng quảng cáo hôm nay. Quay lại vào ngày mai hoặc nâng cấp VIP để xem tiếp.", "ad_limit_reached");
}

/// <summary>Cửa hàng (App Store / Google Play / AdMob) từ chối hoặc không kiểm tra được giao dịch.</summary>
public class StoreVerificationException : Exception
{
    public StoreVerificationException(string message, Exception? innerException = null) : base(message, innerException) { }
}
