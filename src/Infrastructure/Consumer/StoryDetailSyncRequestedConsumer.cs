using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.CrawlStories.Commands.SyncStoryDetail;
using SurveillanceCameras.Application.Local.CrawlStories.IntegrationEvents;
using SurveillanceCameras.Infrastructure.Kafka;
using SurveillanceCameras.Infrastructure.Kafka.Options;

namespace SurveillanceCameras.Infrastructure.Consumer;

/// <summary>
/// Lấy trang chi tiết của từng truyện đã chọn khi đồng bộ. Xử lý tuần tự từng message; mỗi lần gọi tới trang nguồn đi qua
/// bộ giãn cách (<c>requestDelayMs</c> / <c>CrawlStory:HostIntervalsMs</c>), nên các truyện được lấy lần lượt, cách nhau đều.
/// </summary>
public sealed class StoryDetailSyncRequestedConsumer : KafkaConsumerBase<StoryDetailSyncRequestedIntegrationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StoryDetailSyncRequestedConsumer> _logger;

    public StoryDetailSyncRequestedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        IKafkaTopicRegistry topicRegistry,
        ILogger<StoryDetailSyncRequestedConsumer> logger)
        : base(options, topicRegistry, logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task HandleAsync(StoryDetailSyncRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        => CrawlBlockedRetry.RunAsync(async ct =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new SyncStoryDetailCommand(integrationEvent.StorySourceId, integrationEvent.IsNew), ct);
        }, _logger, cancellationToken);
}
