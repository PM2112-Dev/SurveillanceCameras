namespace SurveillanceCameras.Application.Common.Exceptions;

/// <summary>TikTok trả về lỗi, phản hồi rỗng hoặc sai định dạng (cookie hết hạn, bị chặn…).</summary>
public class TikTokApiException : Exception
{
    public TikTokApiException(string message) : base(message) { }

    public TikTokApiException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>TikTok đang chặn máy chủ (trang thử thách WAF, giới hạn tần suất) — thử tiếp chỉ làm bị chặn lâu hơn.</summary>
public class TikTokBlockedException : TikTokApiException
{
    public TikTokBlockedException(string message) : base(message) { }
}

/// <summary>TikTok không cấp file của video này (riêng tư, đã xoá, tập bị khoá/trả phí) — thử lại cũng không được.</summary>
public class TikTokVideoUnavailableException : TikTokApiException
{
    public TikTokVideoUnavailableException(string message) : base(message) { }
}
