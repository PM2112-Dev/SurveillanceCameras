namespace SurveillanceCameras.Infrastructure.Kafka.Options;

/// <summary>
/// Strongly-typed Kafka configuration.
/// Bind from appsettings.json → "Kafka" section.
/// </summary>
public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    /// <summary>Comma-separated list of broker host:port pairs.</summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>Consumer group ID for this service instance.</summary>
    public string ConsumerGroupId { get; set; } = "surveillance-cameras";

    /// <summary>Optional prefix applied to all topic names (e.g. "sc.").</summary>
    public string TopicPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Map of IntegrationEvent type name → Kafka topic name.
    /// Example: { "StoryUpdatedIntegrationEvent": "sc.story.updated" }
    /// </summary>
    public Dictionary<string, string> Topics { get; set; } = new();

    /// <summary>Max retries before routing message to DLQ.</summary>
    public int MaxConsumerRetries { get; set; } = 3;

    /// <summary>Base delay in ms for exponential back-off between retries.</summary>
    public int RetryBaseDelayMs { get; set; } = 500;

    /// <summary>How often (in seconds) the OutboxProcessor polls for pending messages.</summary>
    public int OutboxPollingIntervalSeconds { get; set; } = 5;

    /// <summary>Max batch size per OutboxProcessor poll cycle.</summary>
    public int OutboxBatchSize { get; set; } = 20;
}

