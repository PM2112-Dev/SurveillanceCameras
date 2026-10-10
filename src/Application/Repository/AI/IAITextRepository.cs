using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Repository.AI;

/// <param name="Instructions">Hướng dẫn cho AI (nội dung prompt).</param>
/// <param name="Input">Dữ liệu cần xử lý (tên/giới thiệu truyện…).</param>
public record AITextRequest(AIProvider Provider, string Model, string ApiKey, string Instructions, string Input);

/// <summary>Gửi một yêu cầu sinh văn bản tới nền tảng AI và trả về nội dung trả lời.</summary>
public interface IAITextRepository
{
    /// <exception cref="Common.Exceptions.AIProviderException">Nền tảng trả lỗi, từ chối hoặc trả nội dung rỗng.</exception>
    Task<string> GenerateAsync(AITextRequest request, CancellationToken cancellationToken = default);
}
