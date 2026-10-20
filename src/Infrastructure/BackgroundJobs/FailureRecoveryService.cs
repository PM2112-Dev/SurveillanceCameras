using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Local.Recovery;

namespace SurveillanceCameras.Infrastructure.BackgroundJobs;

/// <summary>
/// Chạy nền: mỗi chu kỳ kiểm tra các nhóm tác vụ đang lỗi (tải chương, biên tập AI, đưa truyện / đăng chương lên TYT);
/// nhóm nào nguyên nhân đã hết (hết bị chặn IP, cookie dùng lại được…) thì đưa tác vụ lỗi vào xử lý lại, chưa hết thì
/// bỏ qua tới chu kỳ sau (xem <see cref="RecoverFailuresCommand"/>).
/// </summary>
public sealed class FailureRecoveryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FailureRecoveryOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FailureRecoveryService> _logger;

    public FailureRecoveryService(IServiceScopeFactory scopeFactory, IOptions<FailureRecoveryOptions> options,
        TimeProvider timeProvider, ILogger<FailureRecoveryService> logger)
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
            _logger.LogInformation("Tự xử lý lại tác vụ lỗi đang tắt (FailureRecovery:Enabled = false)");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));

        // Đợi một chu kỳ trước lượt đầu: lúc khởi động hàng đợi và phiên đăng nhập còn đang nạp.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, _timeProvider, stoppingToken);

                await using var scope = _scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                    new RecoverFailuresCommand(Math.Max(1, _options.MaxPerGroup),
                        TimeSpan.FromMinutes(Math.Max(1, _options.RequeueAfterMinutes))), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Xử lý lại tác vụ lỗi gặp lỗi");
            }
        }
    }
}
