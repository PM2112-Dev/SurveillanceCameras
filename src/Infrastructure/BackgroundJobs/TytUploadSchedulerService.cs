using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Local.Episodes.Commands.ScheduleTytUploads;

namespace SurveillanceCameras.Infrastructure.BackgroundJobs;

/// <summary>Chạy nền: định kỳ đưa chương tới lượt đăng (ngay / theo giờ / theo ngày) vào hàng đợi đăng lên TYT.</summary>
public sealed class TytUploadSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TytUploadOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TytUploadSchedulerService> _logger;

    public TytUploadSchedulerService(IServiceScopeFactory scopeFactory, IOptions<TytUploadOptions> options,
        TimeProvider timeProvider, ILogger<TytUploadSchedulerService> logger)
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
            _logger.LogInformation("Đăng chương lên TYT theo lịch đang tắt (TytUpload:Enabled = false)");
            return;
        }

        var timeZone = ResolveTimeZone(_options.TimeZone);
        var interval = TimeSpan.FromSeconds(Math.Max(10, _options.ScheduleIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                    new ScheduleTytUploadsCommand(timeZone, Math.Max(1, _options.ImmediateBatchSize)), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Lỗi của một lượt (vd. mất kết nối DB) không được dừng hẳn tác vụ.
                _logger.LogError(ex, "Đưa chương vào hàng đợi đăng TYT lỗi");
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

    private TimeZoneInfo ResolveTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            _logger.LogWarning("Không tìm thấy múi giờ {TimeZone} (TytUpload:TimeZone), dùng giờ máy chủ", id);
            return TimeZoneInfo.Local;
        }
    }
}
