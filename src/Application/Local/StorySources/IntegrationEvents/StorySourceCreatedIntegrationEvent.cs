using SurveillanceCameras.Domain.Common;

namespace SurveillanceCameras.Application.Local.StorySources.IntegrationEvents;

/// <summary>
/// Raised when a new StorySource is created. Consumed by <c>WikiDichCrawlConsumer</c>
/// (Infrastructure) to trigger the WikiDich crawl for its enrichment data.
///
/// Lives in Application, not Domain: nothing in Domain ever references this concrete type —
/// only CreateStorySourceCommandHandler constructs it (Domain only needs the abstract
/// IntegrationEvent base, which BaseEntity.AddIntegrationEvent already depends on). Its payload
/// is a boundary contract for external consumers, decided by the use case — the same reason
/// DTOs like StorySourceDto live here rather than in Domain, not a domain invariant.
/// </summary>
public sealed class StorySourceCreatedIntegrationEvent : IntegrationEvent
{
    public required int StorySourceId { get; init; }

    public string? Title { get; init; }

    public int WebSourceId { get; init; }

    /// <summary>Source page URL to crawl; null/empty means nothing to enrich (no crawler for this source yet).</summary>
    public string? LinkRaw { get; init; }
}
