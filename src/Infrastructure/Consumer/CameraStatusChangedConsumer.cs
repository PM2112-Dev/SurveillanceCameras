using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Events;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Infrastructure.Kafka;
using SurveillanceCameras.Infrastructure.Kafka.Options;

namespace SurveillanceCameras.Infrastructure.Consumer;

/// <summary>
/// Consumes CameraStatusChanged events from Kafka and dispatches them
/// as MediatR notifications to re-enter the application layer cleanly.
/// </summary>
public sealed class CameraStatusChangedConsumer : KafkaConsumerBase<CameraStatusChangedIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly IKafkaTopicRegistry _topicRegistry;

    public CameraStatusChangedConsumer(
        IOptions<KafkaOptions> options,
        IKafkaTopicRegistry topicRegistry,
        IMediator mediator,
        ILogger<CameraStatusChangedConsumer> logger)
        : base(options, logger)
    {
        _topicRegistry = topicRegistry;
        _mediator = mediator;
    }

    protected override string Topic => _topicRegistry.GetTopic<CameraStatusChangedIntegrationEvent>();

    protected override async Task HandleAsync(
        CameraStatusChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        // Dispatch to MediatR — keeps consumer thin, business logic stays in Application layer
        await _mediator.Publish(
            new CameraStatusChangedNotification(
                integrationEvent.CameraId,
                integrationEvent.CameraCode,
                integrationEvent.NewStatus,
                integrationEvent.AreaCode),
            cancellationToken);
    }
}

/// <summary>MediatR notification bridging Kafka consumer → Application layer.</summary>
public sealed record CameraStatusChangedNotification(
    int CameraId,
    string CameraCode,
    string NewStatus,
    string? AreaCode) : INotification;

