namespace SurveillanceCameras.Application.Common.Exceptions;

/// <summary>Nền tảng AI trả lỗi hoặc không gọi được (API key sai, hết quota, lỗi mạng…).</summary>
public class AIProviderException : Exception
{
    public AIProviderException(string message) : base(message) { }

    public AIProviderException(string message, Exception innerException) : base(message, innerException) { }
}
