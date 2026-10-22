using System.Collections.Concurrent;

namespace SurveillanceCameras.Application.Local.AIs;

/// <summary>
/// Nhớ API key (AI) nào đang hết quota / bị từ chối tới khi nào, dùng chung cho mọi tác vụ. Nhờ đó mỗi yêu cầu bỏ qua ngay
/// các key đã hết thay vì gọi thử lần lượt từ key đầu (tốn lượt, chậm, và key mới thêm chỉ được dùng sau khi mọi key trước
/// đã thất bại). Chỉ giữ trong bộ nhớ: khởi động lại thì thử lại từ đầu, key đã hồi quota sẽ dùng được ngay.
/// </summary>
public class AIKeyCooldowns
{
    /// <summary>Khoảng nghỉ tối thiểu / tối đa áp cho một key sau lỗi — nền tảng báo vài giờ thì giữ nhưng không quá nửa ngày.</summary>
    public static readonly TimeSpan MinCooldown = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan MaxCooldown = TimeSpan.FromHours(12);

    private readonly ConcurrentDictionary<int, DateTimeOffset> _until = new();

    public void Block(int aiId, TimeSpan duration, DateTimeOffset now)
    {
        var clamped = duration < MinCooldown ? MinCooldown : duration > MaxCooldown ? MaxCooldown : duration;
        _until[aiId] = now + clamped;
    }

    /// <summary>Thời điểm key dùng lại được; <c>null</c> khi key đang dùng được.</summary>
    public DateTimeOffset? BlockedUntil(int aiId, DateTimeOffset now)
        => _until.TryGetValue(aiId, out var until) && until > now ? until : null;

    /// <summary>Gỡ nghỉ của key (key vừa trả lời được, hoặc người dùng sửa key).</summary>
    public void Clear(int aiId) => _until.TryRemove(aiId, out _);
}
