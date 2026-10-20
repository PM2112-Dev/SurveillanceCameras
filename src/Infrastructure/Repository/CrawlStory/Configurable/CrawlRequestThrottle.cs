using System.Collections.Concurrent;

namespace SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;

/// <summary>
/// Giãn cách request tới từng trang nguồn: hai request liên tiếp tới cùng một host cách nhau ít nhất một khoảng, kể cả khi
/// nhiều luồng (đồng bộ song song, consumer lấy nội dung chương…) cùng gọi. Dùng chung cho cả tiến trình nên mọi repository
/// đang chạy đều chia chung một "hàng chờ" theo host. Khi nguồn chặn (429/403) thì nghỉ hẳn một lúc, không gửi thêm.
/// </summary>
public static class CrawlRequestThrottle
{
    private sealed class HostState
    {
        public readonly object Lock = new();
        public DateTimeOffset NextSlot = DateTimeOffset.MinValue;
        public DateTimeOffset BlockedUntil = DateTimeOffset.MinValue;
    }

    private static readonly ConcurrentDictionary<string, HostState> Hosts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Phần ngẫu nhiên cộng thêm vào khoảng cách (0–25%) để nhịp gọi không đều như máy.</summary>
    private const double JitterRatio = 0.25;

    /// <summary>
    /// Chờ tới lượt gọi <paramref name="host"/>. Mỗi lời gọi giữ một "ô giờ" riêng nên các luồng được xếp hàng, mỗi ô cách ô
    /// trước <paramref name="interval"/> (cộng ngẫu nhiên nhẹ). <paramref name="interval"/> bằng 0 thì không chờ.
    /// </summary>
    /// <exception cref="TimeoutException">Host đang trong thời gian nghỉ sau khi bị chặn; <see cref="Exception.Message"/> nói bao giờ gọi lại được.</exception>
    public static async Task WaitAsync(string host, TimeSpan interval, CancellationToken cancellationToken,
        Func<DateTimeOffset>? clock = null, Random? random = null)
    {
        var now = (clock ?? (() => DateTimeOffset.UtcNow))();
        var state = Hosts.GetOrAdd(host, _ => new HostState());

        DateTimeOffset slot;
        lock (state.Lock)
        {
            if (state.BlockedUntil > now)
                throw new TimeoutException($"Đang tạm nghỉ không gọi {host} do nguồn đã chặn request; thử lại sau {state.BlockedUntil.ToLocalTime():HH:mm:ss}.");

            if (interval <= TimeSpan.Zero) return;

            slot = state.NextSlot > now ? state.NextSlot : now;
            var jitter = interval * (random ?? Random.Shared).NextDouble() * JitterRatio;
            state.NextSlot = slot + interval + jitter;
        }

        var wait = slot - now;
        if (wait > TimeSpan.Zero) await Task.Delay(wait, cancellationToken);
    }

    /// <summary>Nghỉ không gọi <paramref name="host"/> trong <paramref name="duration"/> (không rút ngắn thời gian nghỉ đang có).</summary>
    public static void Block(string host, TimeSpan duration, DateTimeOffset? now = null)
    {
        var state = Hosts.GetOrAdd(host, _ => new HostState());
        var until = (now ?? DateTimeOffset.UtcNow) + duration;
        lock (state.Lock)
        {
            if (until > state.BlockedUntil) state.BlockedUntil = until;
        }
    }

    /// <summary>Xoá trạng thái của một host (dùng trong test và khi người dùng chủ động thử lại).</summary>
    public static void Reset(string host) => Hosts.TryRemove(host, out _);
}
