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
    private readonly Lazy<IProducer<string, string>> _producer;
    private readonly IKafkaTopicRegistry _topicRegistry;
    private readonly ILogger<KafkaEventBus> _logger;

    public KafkaEventBus(
        IOptions<KafkaOptions> options,
        IKafkaTopicRegistry topicRegistry,
        ILogger<KafkaEventBus> logger)
    {
        _topicRegistry = topicRegistry;
        _logger = logger;

        // Lazy on purpose: ASP.NET Core's WebApplicationBuilder enables ServiceProviderOptions.
        // ValidateOnBuild in Development, which eagerly constructs every registered service —
        // including this one, via OutboxProcessor's constructor — during builder.Build(). The
        // same thing happens when `dotnet build` runs the Microsoft.Extensions.ApiDescription.Server
        // OpenAPI-doc-generation step, which briefly boots the whole app host. If the producer were
        // built eagerly here, EVERY build/start would open a real broker connection attempt (and,
        // worse, its librdkafka error logs go to raw stderr and fail that MSBuild step outright when
        // no broker is reachable yet). Deferring construction to first actual publish means DI
        // validation only ever touches this wrapper object, never the network.
        var logger1 = _logger;
        _producer = new Lazy<IProducer<string, string>>(() =>
        {
            var config = new ProducerConfig
            {
                BootstrapServers = options.Value.BootstrapServers,
                // Idempotent producer = exactly-once within a single session
                EnableIdempotence = true,
                Acks = Acks.All,
                MessageSendMaxRetries = 5,
                RetryBackoffMs = 200
            };

            return new ProducerBuilder<string, string>(config)
                // Route librdkafka's own logging through ILogger instead of its default raw
                // stderr writer — keeps connection-retry noise out of anything that treats
                // subprocess stderr as a hard failure (see the ValidateOnBuild note above).
                .SetLogHandler((_, logMessage) => logger1.Log(
                    logMessage.Level switch
                    {
                        SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical or SyslogLevel.Error => LogLevel.Warning,
                        SyslogLevel.Warning or SyslogLevel.Notice => LogLevel.Information,
                        _ => LogLevel.Debug
                    },
                    "[rdkafka:{Facility}] {Message}", logMessage.Facility, logMessage.Message))
                .SetErrorHandler((_, error) => logger1.LogWarning(
                    "Kafka producer error: {Reason} (fatal: {IsFatal})", error.Reason, error.IsFatal))
                .Build();
        });
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

        var result = await _producer.Value.ProduceAsync(topic, message, cancellationToken);

        _logger.LogDebug(
            "Published {EventType} [{EventId}] to topic {Topic} partition {Partition} offset {Offset}",
            integrationEvent.EventType, integrationEvent.EventId, topic,
            result.Partition.Value, result.Offset.Value);
    }

    public async Task PublishRawAsync(string topic, string key, string payload, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, string>
        {
            Key = key,
            Value = payload,
            Headers = new Headers
            {
                { "source", System.Text.Encoding.UTF8.GetBytes("outbox-processor") }
            }
        };

        var result = await _producer.Value.ProduceAsync(topic, message, cancellationToken);

        _logger.LogDebug(
            "Published raw payload [{Key}] to topic {Topic} partition {Partition} offset {Offset}",
            key, topic, result.Partition.Value, result.Offset.Value);
    }

    public void Dispose()
    {
        if (_producer.IsValueCreated)
        {
            _producer.Value.Dispose();
        }
    }
}
