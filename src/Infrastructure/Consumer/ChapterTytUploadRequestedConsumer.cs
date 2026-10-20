using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes.Commands.UploadTytChapter;
using SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;
using SurveillanceCameras.Infrastructure.BackgroundJobs;
using SurveillanceCameras.Infrastructure.Kafka;
using SurveillanceCameras.Infrastructure.Kafka.Options;
using SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;

namespace SurveillanceCameras.Infrastructure.Consumer;

/// <summary>Đăng một chương lên TYT. Các chương cách nhau một khoảng (<c>TytUpload:DelayBetweenChaptersSeconds</c>) để không dồn request vào TYT.</summary>
public sealed class ChapterTytUploadRequestedConsumer : KafkaConsumerBase<ChapterTytUploadRequestedIntegrationEvent>
{
    private const string PacingKey = "tyt-chapter-upload";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TytUploadOptions _uploadOptions;

    public ChapterTytUploadRequestedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        IOptions<TytUploadOptions> uploadOptions,
        IKafkaTopicRegistry topicRegistry,
        ILogger<ChapterTytUploadRequestedConsumer> logger)
        : base(options, topicRegistry, logger)
    {
        _scopeFactory = scopeFactory;
        _uploadOptions = uploadOptions.Value;
    }

    protected override async Task HandleAsync(ChapterTytUploadRequestedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        // Dùng chung bộ xếp hàng theo khoá với crawl: các lượt đăng cách nhau tối thiểu một khoảng.
        await CrawlRequestThrottle.WaitAsync(PacingKey,
            TimeSpan.FromSeconds(Math.Max(0, _uploadOptions.DelayBetweenChaptersSeconds)), cancellationToken);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new UploadTytChapterCommand(integrationEvent.ChapterId), cancellationToken);
    }
}
