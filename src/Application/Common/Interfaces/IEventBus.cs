namespace SurveillanceCameras.Application.Common.Interfaces;

/// <summary>
/// Abstraction for publishing integration events to an event bus (Kafka).
/// Application layer depends on this interface — no Kafka dependency here.
/// </summary>
public interface IEventBus
{
    /// <summary>Publish an integration event directly to Kafka.</summary>
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IntegrationEvent;

    /// <summary>
    /// Publish an already-serialized payload to a specific topic, reusing the shared producer.
    /// Used by OutboxProcessor, which only has the JSON persisted in <c>OutboxMessage.Payload</c>
    /// (the original typed event is long gone by the time the outbox is polled) — building a fresh
    /// <c>IProducer</c> per message there would open a new broker connection every poll cycle.
    /// </summary>
    Task PublishRawAsync(string topic, string key, string payload, CancellationToken cancellationToken = default);
}

