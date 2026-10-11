using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;

namespace SurveillanceCameras.Infrastructure.BackgroundJobs;

/// <summary>
/// Chạy nền: tải video của các tập phim chưa có link phát từ TikTok về kho lưu trữ. Tập mới (vừa đồng bộ) được nhận ở lượt
/// quét kế tiếp, nên đồng bộ phim không phải chờ tải video.
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

        var idle = TimeSpan.FromSeconds(Math.Max(5, _options.IntervalSeconds));
        var busy = TimeSpan.FromSeconds(Math.Max(0, _options.DelayBetweenVideosSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            var wait = idle;
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var stored = await scope.ServiceProvider.GetRequiredService<EpisodeVideoStorer>()
                    .StoreNextBatchAsync(stoppingToken);
                // Còn việc thì làm tiếp ngay lô sau.
                if (stored > 0) wait = busy;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (TikTokBlockedException ex)
            {
                wait = TimeSpan.FromMinutes(Math.Max(1, _options.BlockedBackoffMinutes));
                _logger.LogWarning("TikTok đang chặn máy chủ, nghỉ lưu video {Wait}: {Error}", wait, ex.Message);
            }
            catch (Exception ex)
            {
                // Lỗi của một lượt (vd. mất kết nối DB) không được dừng hẳn tác vụ.
                _logger.LogError(ex, "Tự lưu video tập phim lỗi");
            }

            try
            {
                await Task.Delay(wait, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
