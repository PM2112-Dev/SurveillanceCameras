using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Local.StorySources.Commands.EnrichStorySourceFromWikiDich;
using SurveillanceCameras.Application.Local.StorySources.IntegrationEvents;
using SurveillanceCameras.Infrastructure.Kafka;
using SurveillanceCameras.Infrastructure.Kafka.Options;

namespace SurveillanceCameras.Infrastructure.Consumer;

/// <summary>
/// Thin Kafka adapter: on StorySourceCreatedIntegrationEvent, sends
/// EnrichStorySourceFromWikiDichCommand — mirrors how a Web endpoint just calls
/// ISender.Send(...) and leaves the actual work to the Application-layer handler.
/// Registered as a singleton BackgroundService (AddHostedService), so it needs its own
/// per-message DI scope to resolve ISender/IApplicationDbContext (both Scoped).
/// </summary>
public sealed class WikiDichCrawlConsumer : KafkaConsumerBase<StorySourceCreatedIntegrationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;

    protected override string Topic => "storysource-created";

    public WikiDichCrawlConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<WikiDichCrawlConsumer> logger)
        : base(options, logger)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleAsync(StorySourceCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(integrationEvent.LinkRaw))
        {
            // No source page to crawl (e.g. a WebSource without a WikiDich-shaped LinkRaw).
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new EnrichStorySourceFromWikiDichCommand
        {
            StorySourceId = integrationEvent.StorySourceId,
            LinkRaw = integrationEvent.LinkRaw
        }, cancellationToken);
    }
}
