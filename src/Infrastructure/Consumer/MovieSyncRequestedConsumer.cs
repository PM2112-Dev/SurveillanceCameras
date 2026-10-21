using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.Commands.SyncMovieFromTikTok;
using SurveillanceCameras.Application.Local.Movies.IntegrationEvents;
using SurveillanceCameras.Infrastructure.BackgroundJobs;
using SurveillanceCameras.Infrastructure.Kafka;
using SurveillanceCameras.Infrastructure.Kafka.Options;
using SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;

namespace SurveillanceCameras.Infrastructure.Consumer;

/// <summary>Đồng bộ lại một phim từ drama TikTok. Các phim cách nhau <c>MovieDailySync:DelayBetweenDramasSeconds</c> để không dồn request tới TikTok.</summary>
public sealed class MovieSyncRequestedConsumer : KafkaConsumerBase<MovieSyncRequestedIntegrationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MovieDailySyncOptions _syncOptions;

    public MovieSyncRequestedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        IOptions<MovieDailySyncOptions> syncOptions,
        IKafkaTopicRegistry topicRegistry,
        ILogger<MovieSyncRequestedConsumer> logger)
        : base(options, topicRegistry, logger)
    {
        _scopeFactory = scopeFactory;
        _syncOptions = syncOptions.Value;
    }

    protected override async Task HandleAsync(MovieSyncRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await CrawlRequestThrottle.WaitAsync("tiktok-drama-sync",
            TimeSpan.FromSeconds(Math.Max(0, _syncOptions.DelayBetweenDramasSeconds)), cancellationToken);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new SyncMovieFromTikTokCommand(integrationEvent.OwnerId, integrationEvent.UserProfileId, integrationEvent.DramaId),
            cancellationToken);
    }
}
