using System.Collections.Concurrent;
using SurveillanceCameras.Application.Common.Exceptions;

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

        /// <summary>Các ô giờ đã giữ (tăng dần), dùng cho giới hạn "tối đa N request trong một khoảng".</summary>
        public readonly List<DateTimeOffset> Slots = [];
    }

    private static readonly ConcurrentDictionary<string, HostState> Hosts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Phần ngẫu nhiên cộng thêm vào khoảng cách (0–25%) để nhịp gọi không đều như máy.</summary>
    private const double JitterRatio = 0.25;

    /// <summary>
    /// Chờ tới lượt gọi <paramref name="host"/>. Mỗi lời gọi giữ một "ô giờ" riêng nên các luồng được xếp hàng, mỗi ô cách ô
    /// trước <paramref name="interval"/> (cộng ngẫu nhiên nhẹ). <paramref name="interval"/> bằng 0 thì không chờ.
    /// Có <paramref name="maxRequests"/> thì còn giới hạn cửa sổ trượt: trong bất kỳ khoảng <paramref name="window"/> nào
    /// cũng không quá <paramref name="maxRequests"/> request tới host (vd. 5 request mỗi phút).
    /// </summary>
    /// <exception cref="CrawlBlockedException">Host đang trong thời gian nghỉ sau khi bị chặn; <see cref="CrawlBlockedException.RetryAt"/> là lúc gọi lại được.</exception>
    public static async Task WaitAsync(string host, TimeSpan interval, CancellationToken cancellationToken,
        Func<DateTimeOffset>? clock = null, Random? random = null, int maxRequests = 0, TimeSpan window = default)
    {
        var now = (clock ?? (() => DateTimeOffset.UtcNow))();
        var state = Hosts.GetOrAdd(host, _ => new HostState());

        DateTimeOffset slot;
        lock (state.Lock)
        {
            if (state.BlockedUntil > now)
                throw new CrawlBlockedException(
                    $"Đang tạm nghỉ không gọi {host} do nguồn đã chặn request; thử lại sau {state.BlockedUntil.ToLocalTime():HH:mm:ss}.",
                    state.BlockedUntil);

            var limited = maxRequests > 0 && window > TimeSpan.Zero;
            if (interval <= TimeSpan.Zero && !limited) return;

            slot = state.NextSlot > now ? state.NextSlot : now;
            if (limited)
            {
                state.Slots.RemoveAll(x => x <= now - window);
                // Đã đủ N ô trong cửa sổ: ô mới phải đợi tới khi ô thứ (N) tính từ cuối rời khỏi cửa sổ.
                if (state.Slots.Count >= maxRequests)
                {
                    var earliest = state.Slots[state.Slots.Count - maxRequests] + window;
                    if (earliest > slot) slot = earliest;
                }
                state.Slots.Add(slot);
            }

            var jitter = interval * (random ?? Random.Shared).NextDouble() * JitterRatio;
            state.NextSlot = slot + (interval > TimeSpan.Zero ? interval + jitter : TimeSpan.Zero);
        }

        var wait = slot - now;
        if (wait > TimeSpan.Zero) await Task.Delay(wait, cancellationToken);
    }

    /// <summary>Thời điểm sớm nhất có thể gọi <paramref name="host"/> (đang nghỉ do bị chặn); <c>null</c> khi không bị chặn.</summary>
    public static DateTimeOffset? BlockedUntil(string host, DateTimeOffset? now = null)
    {
        if (!Hosts.TryGetValue(host, out var state)) return null;
        lock (state.Lock)
            return state.BlockedUntil > (now ?? DateTimeOffset.UtcNow) ? state.BlockedUntil : null;
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
