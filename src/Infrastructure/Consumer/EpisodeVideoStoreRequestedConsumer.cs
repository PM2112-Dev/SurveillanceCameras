using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideo;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;
using SurveillanceCameras.Application.Local.Movies.IntegrationEvents;
using SurveillanceCameras.Infrastructure.Kafka;
using SurveillanceCameras.Infrastructure.Kafka.Options;
using SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;

namespace SurveillanceCameras.Infrastructure.Consumer;

/// <summary>
/// Tải video một tập từ TikTok lên kho R2. Các tập cách nhau <c>EpisodeVideoStore:DelayBetweenVideosSeconds</c>. TikTok chặn
/// (hoặc chưa đăng nhập) thì nghỉ <c>BlockedBackoffMinutes</c>: các tập chờ trong lúc nghỉ được bỏ qua chứ không gọi tiếp,
/// lượt quét định kỳ sẽ xếp hàng lại khi hết nghỉ.
/// </summary>
public sealed class EpisodeVideoStoreRequestedConsumer : KafkaConsumerBase<EpisodeVideoStoreRequestedIntegrationEvent>
{
    /// <summary>Khoá nghỉ chung với lượt quét định kỳ (<see cref="BlockKey"/> được kiểm tra trước khi xếp hàng).</summary>
    public const string BlockKey = "tiktok-video-download";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EpisodeVideoStoreOptions _storeOptions;
    private readonly ILogger<EpisodeVideoStoreRequestedConsumer> _logger;

    public EpisodeVideoStoreRequestedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        IOptions<EpisodeVideoStoreOptions> storeOptions,
        IKafkaTopicRegistry topicRegistry,
        ILogger<EpisodeVideoStoreRequestedConsumer> logger)
        : base(options, topicRegistry, logger)
    {
        _scopeFactory = scopeFactory;
        _storeOptions = storeOptions.Value;
        _logger = logger;
    }

    protected override async Task HandleAsync(EpisodeVideoStoreRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (CrawlRequestThrottle.BlockedUntil(BlockKey) is { } until)
        {
            _logger.LogInformation("TikTok đang chặn, bỏ qua tập {EpisodeId} tới {Until:HH:mm:ss}", integrationEvent.EpisodeId, until.ToLocalTime());
            return;
        }

        await CrawlRequestThrottle.WaitAsync(BlockKey,
            TimeSpan.FromSeconds(Math.Max(0, _storeOptions.DelayBetweenVideosSeconds)), cancellationToken);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        try
        {
            await sender.Send(new StoreEpisodeVideoCommand(integrationEvent.EpisodeId), cancellationToken);
        }
        catch (TikTokBlockedException ex)
        {
            var backoff = TimeSpan.FromMinutes(Math.Max(1, _storeOptions.BlockedBackoffMinutes));
            CrawlRequestThrottle.Block(BlockKey, backoff);
            _logger.LogWarning("TikTok đang chặn máy chủ, nghỉ tải video {Backoff}: {Error}", backoff, ex.Message);
        }
    }
}
