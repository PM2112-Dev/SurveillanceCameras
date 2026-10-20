using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes.Commands.SyncWorkEpisodesPage;
using SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;
using SurveillanceCameras.Infrastructure.Kafka;
using SurveillanceCameras.Infrastructure.Kafka.Options;

namespace SurveillanceCameras.Infrastructure.Consumer;

/// <summary>Đồng bộ một trang mục lục chương; trang sau được phát thành message mới (xem SyncWorkEpisodesPageCommand).</summary>
public sealed class StoryChaptersSyncRequestedConsumer : KafkaConsumerBase<StoryChaptersSyncRequestedIntegrationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    public StoryChaptersSyncRequestedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        IKafkaTopicRegistry topicRegistry,
        ILogger<StoryChaptersSyncRequestedConsumer> logger)
        : base(options, topicRegistry, logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task HandleAsync(StoryChaptersSyncRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        => CrawlBlockedRetry.RunAsync(async ct =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await sender.Send(new SyncWorkEpisodesPageCommand(integrationEvent.StoryId, integrationEvent.Page), ct);
        }, _logger, cancellationToken);
}
