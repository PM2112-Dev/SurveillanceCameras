using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.Commands.SyncChannelMovies;
using SurveillanceCameras.Application.Local.Movies.IntegrationEvents;
using SurveillanceCameras.Infrastructure.BackgroundJobs;
using SurveillanceCameras.Infrastructure.Kafka;
using SurveillanceCameras.Infrastructure.Kafka.Options;
using SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;

namespace SurveillanceCameras.Infrastructure.Consumer;

/// <summary>Kiểm tra một kênh TikTok: đọc danh sách drama và xếp hàng đồng bộ từng phim. Các kênh cách nhau như các drama.</summary>
public sealed class ChannelSyncRequestedConsumer : KafkaConsumerBase<ChannelSyncRequestedIntegrationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MovieDailySyncOptions _syncOptions;

    public ChannelSyncRequestedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        IOptions<MovieDailySyncOptions> syncOptions,
        IKafkaTopicRegistry topicRegistry,
        ILogger<ChannelSyncRequestedConsumer> logger)
        : base(options, topicRegistry, logger)
    {
        _scopeFactory = scopeFactory;
        _syncOptions = syncOptions.Value;
    }

    protected override async Task HandleAsync(ChannelSyncRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(Math.Max(0, _syncOptions.DelayBetweenDramasSeconds));
        await CrawlRequestThrottle.WaitAsync("tiktok-drama-sync", delay, cancellationToken);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new SyncChannelMoviesCommand(integrationEvent.OwnerId, integrationEvent.UserProfileId, delay), cancellationToken);
    }
}
