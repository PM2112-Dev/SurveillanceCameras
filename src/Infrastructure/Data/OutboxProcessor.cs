using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
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
                // Reuse the singleton KafkaEventBus producer (one long-lived connection) instead of
                // opening a fresh IProducer per message — see IEventBus.PublishRawAsync for why.
                await _eventBus.PublishRawAsync(message.Topic, message.Id.ToString(), message.Payload, ct);
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
}

