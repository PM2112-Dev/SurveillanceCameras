using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Infrastructure.Kafka.Options;
using System.Text.Json;

namespace SurveillanceCameras.Infrastructure.Kafka;

/// <summary>
/// Abstract base for all Kafka consumers.
/// Handles: bootstrap, consume loop, deserialization, retry with exponential back-off, DLQ routing.
/// Concrete consumers override <see cref="HandleAsync"/> to process the typed event.
/// </summary>
/// <typeparam name="TEvent">The integration event type this consumer handles.</typeparam>
public abstract class KafkaConsumerBase<TEvent> : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IProducer<string, string> _dlqProducer;
    private readonly KafkaOptions _options;
    private readonly ILogger _logger;

    protected abstract string Topic { get; }

    protected KafkaConsumerBase(IOptions<KafkaOptions> options, ILogger logger)
    {
        _options = options.Value;
        _logger = logger;

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            // Manual commit — only commit after successful processing
            EnableAutoCommit = false
        };
        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

        var producerConfig = new ProducerConfig { BootstrapServers = _options.BootstrapServers };
        _dlqProducer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(Topic);
        _logger.LogInformation("{Consumer} subscribed to topic [{Topic}]", GetType().Name, Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = _consumer.Consume(stoppingToken);
                    if (result?.Message is null) continue;

                    await ProcessWithRetryAsync(result, stoppingToken);
                    _consumer.Commit(result);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Consumer} unhandled error consuming from {Topic}", GetType().Name, Topic);
                    // Short pause to avoid tight error loop
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    private async Task ProcessWithRetryAsync(ConsumeResult<string, string> result, CancellationToken ct)
    {
        var maxRetries = _options.MaxConsumerRetries;
        var attempt = 0;

        while (true)
        {
            try
            {
                var evt = JsonSerializer.Deserialize<TEvent>(result.Message.Value)
                          ?? throw new InvalidOperationException($"Failed to deserialize {typeof(TEvent).Name}");

                await HandleAsync(evt, ct);
                return;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                attempt++;
                if (attempt > maxRetries)
                {
                    _logger.LogError(ex,
                        "{Consumer} failed after {Retries} retries. Routing to DLQ. Key={Key}",
                        GetType().Name, maxRetries, result.Message.Key);

                    await RouteToDlqAsync(result, ex);
                    return;
                }

                var delay = _options.RetryBaseDelayMs * (int)Math.Pow(2, attempt - 1); // exponential back-off
                _logger.LogWarning(ex,
                    "{Consumer} attempt {Attempt}/{MaxRetries} failed. Retrying in {Delay}ms...",
                    GetType().Name, attempt, maxRetries, delay);
                await Task.Delay(delay, ct);
            }
        }
    }

    private async Task RouteToDlqAsync(ConsumeResult<string, string> result, Exception ex)
    {
        var dlqTopic = $"{Topic}.dlq";
        var headers = new Headers
        {
            { "x-original-topic", System.Text.Encoding.UTF8.GetBytes(Topic) },
            { "x-exception-type", System.Text.Encoding.UTF8.GetBytes(ex.GetType().Name) },
            { "x-exception-message", System.Text.Encoding.UTF8.GetBytes(ex.Message[..Math.Min(ex.Message.Length, 500)]) },
            { "x-failed-on", System.Text.Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")) }
        };

        // Copy original headers
        if (result.Message.Headers is not null)
            foreach (var h in result.Message.Headers)
                headers.Add(h.Key, h.GetValueBytes());

        await _dlqProducer.ProduceAsync(dlqTopic, new Message<string, string>
        {
            Key = result.Message.Key,
            Value = result.Message.Value,
            Headers = headers
        });

        _logger.LogWarning("Message routed to DLQ topic [{DlqTopic}] Key={Key}", dlqTopic, result.Message.Key);
    }

    /// <summary>Override to process a strongly-typed integration event.</summary>
    protected abstract Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);

    public override void Dispose()
    {
        _consumer.Dispose();
        _dlqProducer.Dispose();
        base.Dispose();
    }
}

