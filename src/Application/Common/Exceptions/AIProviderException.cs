namespace SurveillanceCameras.Application.Common.Exceptions;

/// <summary>Nền tảng AI trả lỗi hoặc không gọi được (API key sai, hết quota, lỗi mạng…).</summary>
public class AIProviderException : Exception
{
    public AIProviderException(string message) : base(message) { }

    public AIProviderException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// API key tạm thời không dùng được: hết quota/giới hạn tần suất (429) hoặc bị từ chối (401/403). Gọi lại bằng key này trước
/// <see cref="RetryAfter"/> chỉ tốn lượt và thất bại — nên chuyển sang key khác.
/// </summary>
public class AIKeyUnavailableException : AIProviderException
{
    public AIKeyUnavailableException(string message, TimeSpan retryAfter) : base(message) => RetryAfter = retryAfter;

    /// <summary>Bao lâu nữa key dùng lại được (nền tảng báo, hoặc mặc định theo loại lỗi).</summary>
    public TimeSpan RetryAfter { get; }
}
