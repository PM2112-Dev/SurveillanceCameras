namespace SurveillanceCameras.Domain.Common;

/// <summary>
/// Outbox pattern: persisted alongside domain state changes in the same DB transaction.
/// A background processor reads these and publishes to Kafka, guaranteeing at-least-once delivery.
/// </summary>
public class OutboxMessage
{
    private OutboxMessage() { } // EF Core

    public static OutboxMessage Create(string eventType, string topic, string payload) => new()
    {
        Id = Guid.NewGuid(),
        EventType = eventType,
        Topic = topic,
        Payload = payload,
        OccurredOn = DateTimeOffset.UtcNow
    };

    public Guid Id { get; private set; }

    /// <summary>Assembly-qualified or simple type name for deserialization.</summary>
    public string EventType { get; private set; } = default!;

    /// <summary>Kafka topic to publish to.</summary>
    public string Topic { get; private set; } = default!;

    /// <summary>JSON-serialized IntegrationEvent payload.</summary>
    public string Payload { get; private set; } = default!;

    public DateTimeOffset OccurredOn { get; private set; }

    /// <summary>Set when successfully published to Kafka.</summary>
    public DateTimeOffset? ProcessedOn { get; private set; }

    /// <summary>Last error message if processing failed.</summary>
    public string? Error { get; private set; }

    /// <summary>Number of processing attempts.</summary>
    public int RetryCount { get; private set; }

    public void MarkProcessed() => ProcessedOn = DateTimeOffset.UtcNow;

    public void MarkFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}

