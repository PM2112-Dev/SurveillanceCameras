using SurveillanceCameras.Domain.Common;

namespace SurveillanceCameras.Application.Local.StorySources.IntegrationEvents;

/// <summary>
/// Phát khi có nguồn truyện mới (đồng bộ từ trang nguồn) hoặc khi người dùng yêu cầu tạo truyện bằng AI.
/// Consumer <c>StorySourceCreatedConsumer</c> (Infrastructure) dùng AI biên tập tên và giới thiệu rồi tạo Story.
///
/// Nằm ở Application, không ở Domain: payload là hợp đồng với consumer do use case quyết định, giống DTO.
/// </summary>
public sealed class StorySourceCreatedIntegrationEvent : IntegrationEvent
{
    public required int StorySourceId { get; init; }

    public string? Title { get; init; }

    public int WebSourceId { get; init; }

    /// <summary>Source page URL to crawl; null/empty means nothing to enrich (no crawler for this source yet).</summary>
    public string? LinkRaw { get; init; }
}
