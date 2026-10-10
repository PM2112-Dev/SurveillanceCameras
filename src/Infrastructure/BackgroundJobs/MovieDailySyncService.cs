using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Local.Movies.Commands.ResyncAllMovies;

namespace SurveillanceCameras.Infrastructure.BackgroundJobs;

/// <summary>Mỗi ngày vào <see cref="MovieDailySyncOptions.TimeOfDay"/> đồng bộ lại mọi phim (lượt xem, tập mới).</summary>
public sealed class MovieDailySyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MovieDailySyncOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MovieDailySyncService> _logger;

    public MovieDailySyncService(IServiceScopeFactory scopeFactory, IOptions<MovieDailySyncOptions> options,
        TimeProvider timeProvider, ILogger<MovieDailySyncService> logger)
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
            _logger.LogInformation("Tự đồng bộ phim hằng ngày đang tắt (MovieDailySync:Enabled = false)");
            return;
        }

        if (!TimeOnly.TryParse(_options.TimeOfDay, out var timeOfDay))
        {
            _logger.LogError("MovieDailySync:TimeOfDay không hợp lệ: {Value} (cần dạng HH:mm) — không tự đồng bộ phim", _options.TimeOfDay);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = NextRunDelay(_timeProvider.GetLocalNow(), timeOfDay);
            _logger.LogInformation("Lần tự đồng bộ phim tiếp theo sau {Delay}", delay);

            try
            {
                await Task.Delay(delay, _timeProvider, stoppingToken);

                await using var scope = _scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                await sender.Send(new ResyncAllMoviesCommand(TimeSpan.FromSeconds(Math.Max(0, _options.DelayBetweenDramasSeconds))),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Lỗi của một lượt (vd. mất kết nối DB) không được dừng hẳn tác vụ: đợi tới ngày hôm sau.
                _logger.LogError(ex, "Tự đồng bộ phim lỗi");
            }
        }
    }

    /// <summary>Thời gian tới lần chạy kế tiếp vào <paramref name="timeOfDay"/> (hôm nay nếu chưa qua, không thì ngày mai).</summary>
    public static TimeSpan NextRunDelay(DateTimeOffset now, TimeOnly timeOfDay)
    {
        var next = new DateTimeOffset(DateOnly.FromDateTime(now.DateTime), timeOfDay, now.Offset);
        if (next <= now) next = next.AddDays(1);
        return next - now;
    }
}
