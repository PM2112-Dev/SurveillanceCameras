using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Works.Commands.CreateTytStory;
using SurveillanceCameras.Application.Local.Works.IntegrationEvents;
using SurveillanceCameras.Infrastructure.Kafka;
using SurveillanceCameras.Infrastructure.Kafka.Options;

namespace SurveillanceCameras.Infrastructure.Consumer;

/// <summary>Đưa truyện lên TYT: tạo (lưu Id TYT) rồi cập nhật thông tin truyện.</summary>
public sealed class TytStoryCreateRequestedConsumer : KafkaConsumerBase<TytStoryCreateRequestedIntegrationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TytStoryCreateRequestedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        IKafkaTopicRegistry topicRegistry,
        ILogger<TytStoryCreateRequestedConsumer> logger)
        : base(options, topicRegistry, logger)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleAsync(TytStoryCreateRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new CreateTytStoryCommand(integrationEvent.StoryId), cancellationToken);
    }
}
