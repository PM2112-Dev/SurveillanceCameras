using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Repository.AI;

/// <param name="Id">ID model dùng khi gọi API (vd. <c>gemini-2.5-flash</c>, <c>claude-opus-5</c>).</param>
/// <param name="DisplayName">Tên hiển thị nếu nền tảng có trả về.</param>
public record AIModelDto(string Id, string? DisplayName);

/// <summary>Lấy danh sách model mà API key dùng được, gọi thẳng API của nền tảng.</summary>
public interface IAIModelRepository
{
    Task<List<AIModelDto>> ListModelsAsync(AIProvider provider, string apiKey, CancellationToken cancellationToken = default);
}
