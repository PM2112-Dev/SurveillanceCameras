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
}

