using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Infrastructure.Data;
using SurveillanceCameras.Infrastructure.Kafka.Options;

namespace SurveillanceCameras.Infrastructure.Data;

/// <summary>
/// Background service that polls the OutboxMessages table and publishes pending messages to Kafka.
/// Implements the "polling publisher" variant of the Outbox Pattern.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventBus _eventBus;
    private readonly KafkaOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IEventBus eventBus,
        IOptions<KafkaOptions> options,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _eventBus = eventBus;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started. Polling every {Interval}s.", _options.OutboxPollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessor encountered an error.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.OutboxPollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedOn == null && m.RetryCount < _options.MaxConsumerRetries)
            .OrderBy(m => m.OccurredOn)
            .Take(_options.OutboxBatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        _logger.LogDebug("OutboxProcessor processing {Count} messages.", pending.Count);

        foreach (var message in pending)
        {
            try
            {
                // Publish via KafkaEventBus using a raw producer call
                // We bypass the typed generic here by producing directly
                await PublishRawAsync(message.Topic, message.Payload, message.EventType, ct);
                message.MarkProcessed();

                _logger.LogDebug("OutboxMessage {Id} ({EventType}) published to [{Topic}].",
                    message.Id, message.EventType, message.Topic);
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.Message);
                _logger.LogWarning(ex,
                    "OutboxMessage {Id} failed (attempt {RetryCount}). Will retry.",
                    message.Id, message.RetryCount);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task PublishRawAsync(string topic, string payload, string eventType, CancellationToken ct)
    {
        // Use the IEventBus-backed producer directly via Kafka client
        // We get the producer from a dedicated field to avoid double-serialization
        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All
        };

        using var producer = new ProducerBuilder<string, string>(config).Build();

        var message = new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = payload,
            Headers = new Headers
            {
                { "event-type", System.Text.Encoding.UTF8.GetBytes(eventType) },
                { "source", System.Text.Encoding.UTF8.GetBytes("outbox-processor") }
            }
        };

        await producer.ProduceAsync(topic, message, ct);
    }
}

