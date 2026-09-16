using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.WikiDichService;

namespace SurveillanceCameras.Application.Local.StorySources.Commands.EnrichStorySourceFromWikiDich;

/// <summary>
/// Fills in a StorySource's crawlable fields (title/author/description/image/total chapters)
/// by scraping its LinkRaw via WikiDich. Triggered asynchronously by WikiDichCrawlConsumer
/// (Infrastructure) when it receives a StorySourceCreatedIntegrationEvent from Kafka — this is
/// the actual business logic; the consumer is just a thin adapter that calls this command.
/// </summary>
public record EnrichStorySourceFromWikiDichCommand : IRequest
{
    public required int StorySourceId { get; init; }

    public required string LinkRaw { get; init; }
}

public class EnrichStorySourceFromWikiDichCommandHandler : IRequestHandler<EnrichStorySourceFromWikiDichCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IWikiDichApiService _wikiDichApiService;
    private readonly ILogger<EnrichStorySourceFromWikiDichCommandHandler> _logger;

    public EnrichStorySourceFromWikiDichCommandHandler(
        IApplicationDbContext context,
        IWikiDichApiService wikiDichApiService,
        ILogger<EnrichStorySourceFromWikiDichCommandHandler> logger)
    {
        _context = context;
        _wikiDichApiService = wikiDichApiService;
        _logger = logger;
    }

    public async Task Handle(EnrichStorySourceFromWikiDichCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StorySources
            .FirstOrDefaultAsync(x => x.Id == request.StorySourceId, cancellationToken);

        if (entity is null)
        {
            // The row may have been deleted between the event being queued and this consumer
            // picking it up — not an error worth retrying/DLQ-ing, just skip.
            _logger.LogInformation(
                "EnrichStorySourceFromWikiDich: StorySource {Id} no longer exists, skipping.",
                request.StorySourceId);
            return;
        }

        // Let a scrape failure propagate: KafkaConsumerBase retries with back-off, then routes
        // to the DLQ topic after MaxConsumerRetries — don't swallow it here.
        var data = await _wikiDichApiService.FetchStory(request.LinkRaw, cancellationToken);

        entity.Title ??= data.Title;
        entity.Author ??= data.Author;
        entity.Description ??= data.Description;
        entity.ImageUrl ??= data.ImageUrl;
        entity.TotalChapters ??= data.TotalChapters;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "EnrichStorySourceFromWikiDich: StorySource {Id} enriched from {Link} ({ChapterCount} chapters).",
            request.StorySourceId, request.LinkRaw, data.TotalChapters);
    }
}
