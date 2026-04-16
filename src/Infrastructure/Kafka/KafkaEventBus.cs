using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Common;
using SurveillanceCameras.Infrastructure.Kafka.Options;
using System.Text.Json;

namespace SurveillanceCameras.Infrastructure.Kafka;

/// <summary>
/// Direct Kafka producer implementing <see cref="IEventBus"/>.
/// Used by OutboxProcessor for reliable publishing (outbox → Kafka).
/// Can also be used for fire-and-forget scenarios.
/// </summary>
public sealed class KafkaEventBus : IEventBus, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly IKafkaTopicRegistry _topicRegistry;
    private readonly ILogger<KafkaEventBus> _logger;

    public KafkaEventBus(
        IOptions<KafkaOptions> options,
        IKafkaTopicRegistry topicRegistry,
        ILogger<KafkaEventBus> logger)
    {
        _topicRegistry = topicRegistry;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            // Idempotent producer = exactly-once within a single session
            EnableIdempotence = true,
            Acks = Acks.All,
            MessageSendMaxRetries = 5,
            RetryBackoffMs = 200
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
    {
        var topic = _topicRegistry.GetTopic<T>();
        var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());

        var message = new Message<string, string>
        {
            Key = integrationEvent.EventId.ToString(),
            Value = payload,
            Headers = new Headers
            {
                { "event-type", System.Text.Encoding.UTF8.GetBytes(integrationEvent.EventType) },
                { "occurred-on", System.Text.Encoding.UTF8.GetBytes(integrationEvent.OccurredOn.ToString("O")) }
            }
        };

        var result = await _producer.ProduceAsync(topic, message, cancellationToken);

        _logger.LogDebug(
            "Published {EventType} [{EventId}] to topic {Topic} partition {Partition} offset {Offset}",
            integrationEvent.EventType, integrationEvent.EventId, topic,
            result.Partition.Value, result.Offset.Value);
    }

    public void Dispose() => _producer.Dispose();
}

