namespace SurveillanceCameras.Application.Common.Interfaces;

/// <summary>
/// Maps integration event types to their Kafka topic names.
/// Keeps topic name configuration out of event/handler code.
/// </summary>
public interface IKafkaTopicRegistry
{
    /// <summary>Get topic name for a given integration event type.</summary>
    string GetTopic<T>() where T : IntegrationEvent;

    /// <summary>Get topic name by event type string (used by OutboxProcessor).</summary>
    string GetTopic(string eventType);
}

