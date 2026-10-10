using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Works.Commands.CreateWorkFromStorySource;
using SurveillanceCameras.Application.Local.StorySources.IntegrationEvents;
using SurveillanceCameras.Infrastructure.Kafka;
using SurveillanceCameras.Infrastructure.Kafka.Options;

namespace SurveillanceCameras.Infrastructure.Consumer;

/// <summary>
/// Nguồn truyện mới → dùng AI biên tập tên/giới thiệu và tạo Story. Lỗi (AI hết quota, lỗi mạng…) được
/// <see cref="KafkaConsumerBase{TEvent}"/> thử lại với back-off rồi đưa vào DLQ; lỗi cuối cùng cũng được lưu
/// vào <c>StorySource.StoryCreationError</c> để hiện trên giao diện.
/// </summary>
public sealed class StorySourceCreatedConsumer : KafkaConsumerBase<StorySourceCreatedIntegrationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public StorySourceCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        IKafkaTopicRegistry topicRegistry,
        ILogger<StorySourceCreatedConsumer> logger)
        : base(options, topicRegistry, logger)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleAsync(StorySourceCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new CreateWorkFromStorySourceCommand(integrationEvent.StorySourceId), cancellationToken);
    }
}
