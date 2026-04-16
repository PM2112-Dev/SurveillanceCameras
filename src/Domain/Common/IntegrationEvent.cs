namespace SurveillanceCameras.Domain.Common;

/// <summary>
/// Base class for integration events published to Kafka (cross-service messaging).
/// Distinct from BaseEvent (in-process domain events via MediatR).
/// </summary>
public abstract class IntegrationEvent
{
    protected IntegrationEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        EventType = GetType().Name;
    }

    /// <summary>Unique identifier for idempotency checks.</summary>
    public Guid EventId { get; init; }

    /// <summary>When the event occurred.</summary>
    public DateTimeOffset OccurredOn { get; init; }

    /// <summary>Discriminator for deserialization.</summary>
    public string EventType { get; init; }
}

