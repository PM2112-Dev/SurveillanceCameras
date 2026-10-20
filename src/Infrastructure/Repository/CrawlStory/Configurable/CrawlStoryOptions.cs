namespace SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;

/// <summary>Cấu hình chung của crawl truyện (mục <c>CrawlStory</c> trong appsettings).</summary>
public class CrawlStoryOptions
{
    public const string SectionName = "CrawlStory";

    /// <summary>
    /// Khoảng cách tối thiểu (mili giây) giữa hai request tới cùng một host. Khai báo theo host (vd. <c>wikicv.org</c>) để áp
    /// cả cho nguồn web đã lưu cấu hình crawl cũ chưa có <c>requestDelayMs</c>; nguồn khai báo <c>requestDelayMs</c> thì lấy giá trị lớn hơn.
    /// </summary>
    public Dictionary<string, int> HostIntervalsMs { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Khoảng cách tối thiểu cho host không có trong <see cref="HostIntervalsMs"/>; 0 là không giãn cách.</summary>
    public int DefaultIntervalMs { get; set; }

    /// <summary>Số giây không gọi một host sau khi nó trả 429/403 (nguồn không gửi Retry-After). Gọi tiếp lúc bị chặn dễ bị cấm lâu hơn.</summary>
    public int BlockedCooldownSeconds { get; set; } = 600;

    /// <summary>Khoảng cách áp cho <paramref name="host"/>: lớn nhất giữa cấu hình chung và <paramref name="sourceMs"/> của nguồn web.</summary>
    public TimeSpan IntervalFor(string host, int? sourceMs)
    {
        var fromHost = HostIntervalsMs.TryGetValue(host, out var ms) ? ms : DefaultIntervalMs;
        return TimeSpan.FromMilliseconds(Math.Max(0, Math.Max(fromHost, sourceMs ?? 0)));
    }
}
