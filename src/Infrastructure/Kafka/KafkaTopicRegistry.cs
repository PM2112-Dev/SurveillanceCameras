using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Common;
using SurveillanceCameras.Infrastructure.Kafka.Options;

namespace SurveillanceCameras.Infrastructure.Kafka;

/// <summary>
/// Resolves Kafka topic names from configuration.
/// Falls back to a convention-based name if not explicitly configured.
/// </summary>
public sealed class KafkaTopicRegistry : IKafkaTopicRegistry
{
    private readonly KafkaOptions _options;

    public KafkaTopicRegistry(IOptions<KafkaOptions> options) => _options = options.Value;

    public string GetTopic<T>() where T : IntegrationEvent
        => GetTopic(typeof(T).Name);

    public string GetTopic(string eventType)
    {
        if (_options.Topics.TryGetValue(eventType, out var topic))
            return topic;

        // Convention fallback: prefix + kebab-case of event type name
        var kebab = ToKebabCase(eventType.Replace("IntegrationEvent", string.Empty));
        return $"{_options.TopicPrefix}{kebab}";
    }

    private static string ToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return string.Concat(input.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "-" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
    }
}

