using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Infrastructure.Data;
using SurveillanceCameras.Infrastructure.Kafka.Options;

namespace SurveillanceCameras.Infrastructure.Kafka;

/// <summary>
/// Extension methods for wiring all Kafka services into the DI container.
/// Call from Infrastructure <see cref="DependencyInjection.AddInfrastructureServices"/>.
/// </summary>
public static class KafkaDependencyInjection
{
    public static void AddKafkaServices(this IHostApplicationBuilder builder)
    {
        // 1. Bind strongly-typed options
        builder.Services.Configure<KafkaOptions>(
            builder.Configuration.GetSection(KafkaOptions.SectionName));

        // 2. Topic registry
        builder.Services.AddSingleton<IKafkaTopicRegistry, KafkaTopicRegistry>();

        // 3. Event bus (direct producer — used by OutboxProcessor)
        builder.Services.AddSingleton<IEventBus, KafkaEventBus>();

        // 4. Outbox processor background service
        builder.Services.AddHostedService<OutboxProcessor>();

        // 5. Consumers — each is an IHostedService (BackgroundService)
        //    Add new consumers here as the system grows
    }
}

