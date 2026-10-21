using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Local.Movies.Commands.QueueEpisodeVideoStorage;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;
using SurveillanceCameras.Infrastructure.Consumer;
using SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;

namespace SurveillanceCameras.Infrastructure.BackgroundJobs;

/// <summary>
/// Chạy nền: định kỳ tìm các tập phim chưa có video trên kho R2 và đưa vào hàng đợi Kafka (consumer
/// <see cref="EpisodeVideoStoreRequestedConsumer"/> tải và lưu từng tập). Chỉ xếp hàng khi kho của người dùng còn chỗ và
/// TikTok không đang chặn; tập lỗi quá số lần được mở lại thử sau một khoảng — nên cũng là bước tự kiểm tra lại định kỳ.
/// </summary>
public sealed class EpisodeVideoStoreService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EpisodeVideoStoreOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EpisodeVideoStoreService> _logger;

    public EpisodeVideoStoreService(IServiceScopeFactory scopeFactory, IOptions<EpisodeVideoStoreOptions> options,
        TimeProvider timeProvider, ILogger<EpisodeVideoStoreService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Tự lưu video tập phim từ TikTok đang tắt (EpisodeVideoStore:Enabled = false)");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(5, _options.IntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (CrawlRequestThrottle.BlockedUntil(EpisodeVideoStoreRequestedConsumer.BlockKey) is { } until)
                {
                    _logger.LogInformation("TikTok đang chặn máy chủ, chưa xếp hàng tải video tới {Until:HH:mm:ss}", until.ToLocalTime());
                }
                else
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var queued = await scope.ServiceProvider.GetRequiredService<ISender>()
                        .Send(new QueueEpisodeVideoStorageCommand(), stoppingToken);
                    if (queued > 0) _logger.LogInformation("Đã đưa {Count} tập vào hàng đợi lưu video lên kho", queued);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Lỗi của một lượt (vd. mất kết nối DB) không được dừng hẳn tác vụ.
                _logger.LogError(ex, "Xếp hàng lưu video tập phim lỗi");
            }

            try
            {
                await Task.Delay(interval, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
