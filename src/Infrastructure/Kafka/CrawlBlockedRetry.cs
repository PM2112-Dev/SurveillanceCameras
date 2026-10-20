using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;

namespace SurveillanceCameras.Infrastructure.Kafka;

/// <summary>
/// Cho consumer crawl: nguồn đang chặn request thì chờ hết thời gian nghỉ rồi làm lại, thay vì thất bại (và bị đưa vào DLQ)
/// sau vài lần thử cách nhau vài giây. Consumer xử lý tuần tự nên chờ ở đây cũng là giữ nhịp gọi cho cả hàng chờ.
/// </summary>
public static class CrawlBlockedRetry
{
    /// <summary>Mỗi lần chờ tối đa bấy nhiêu: dưới <c>MaxPollIntervalMs</c> của consumer để không bị đẩy khỏi nhóm.</summary>
    public static readonly TimeSpan MaxWait = TimeSpan.FromMinutes(10);

    /// <summary>Số lần chờ tối đa; quá thì ném lỗi cho cơ chế thử lại / DLQ của consumer.</summary>
    public const int MaxWaits = 2;

    public static async Task RunAsync(Func<CancellationToken, Task> action, ILogger logger, CancellationToken cancellationToken,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        delay ??= Task.Delay;
        for (var waits = 0; ; waits++)
        {
            try
            {
                await action(cancellationToken);
                return;
            }
            catch (CrawlBlockedException ex) when (waits < MaxWaits)
            {
                var wait = ex.RetryAt - DateTimeOffset.UtcNow;
                if (wait < TimeSpan.FromSeconds(1)) wait = TimeSpan.FromSeconds(1);
                if (wait > MaxWait) wait = MaxWait;
                logger.LogWarning("Nguồn đang chặn request, chờ {Seconds:F0} giây rồi làm lại ({Attempt}/{Max}): {Message}",
                    wait.TotalSeconds, waits + 1, MaxWaits, ex.Message);
                await delay(wait, cancellationToken);
            }
        }
    }
}
