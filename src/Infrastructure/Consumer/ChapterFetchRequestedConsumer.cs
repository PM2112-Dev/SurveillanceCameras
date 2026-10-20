using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes.Commands.FetchEpisodeContent;
using SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;
using SurveillanceCameras.Infrastructure.Kafka;
using SurveillanceCameras.Infrastructure.Kafka.Options;

namespace SurveillanceCameras.Infrastructure.Consumer;

/// <summary>Tải nội dung gốc của một chương. Xử lý tuần tự từng message nên tự giãn nhịp gọi tới trang nguồn.</summary>
public sealed class ChapterFetchRequestedConsumer : KafkaConsumerBase<ChapterFetchRequestedIntegrationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    public ChapterFetchRequestedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        IKafkaTopicRegistry topicRegistry,
        ILogger<ChapterFetchRequestedConsumer> logger)
        : base(options, topicRegistry, logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task HandleAsync(ChapterFetchRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        => CrawlBlockedRetry.RunAsync(async ct =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await sender.Send(new FetchEpisodeContentCommand(integrationEvent.ChapterId), ct);
        }, _logger, cancellationToken);
}
