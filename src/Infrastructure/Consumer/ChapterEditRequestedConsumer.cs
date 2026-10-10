using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes.Commands.EditEpisodeContent;
using SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;
using SurveillanceCameras.Infrastructure.Kafka;
using SurveillanceCameras.Infrastructure.Kafka.Options;

namespace SurveillanceCameras.Infrastructure.Consumer;

/// <summary>AI biên tập nội dung một chương.</summary>
public sealed class ChapterEditRequestedConsumer : KafkaConsumerBase<ChapterEditRequestedIntegrationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ChapterEditRequestedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        IKafkaTopicRegistry topicRegistry,
        ILogger<ChapterEditRequestedConsumer> logger)
        : base(options, topicRegistry, logger)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleAsync(ChapterEditRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new EditEpisodeContentCommand(integrationEvent.ChapterId, integrationEvent.Force), cancellationToken);
    }
}
