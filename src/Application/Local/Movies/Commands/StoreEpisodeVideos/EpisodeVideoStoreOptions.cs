namespace SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;

/// <summary>Cấu hình tác vụ tự lưu video tập phim về kho (mục <c>EpisodeVideoStore</c> trong appsettings).</summary>
public class EpisodeVideoStoreOptions
{
    public const string SectionName = "EpisodeVideoStore";

    /// <summary>
    /// Bật thì tác vụ nền tự tải video của tập phim từ TikTok về kho. TikTok không cấp file của tập drama cho phiên chưa
    /// đăng nhập và có thể chặn máy chủ sau vài lượt tải (khảo sát 2026-10-11), nên cần một tài khoản TikTok còn đăng nhập.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Nghỉ giữa hai lượt quét khi không còn tập nào chờ lưu (giây).</summary>
    public int IntervalSeconds { get; set; } = 60;

    /// <summary>Số tập xử lý mỗi lượt.</summary>
    public int BatchSize { get; set; } = 5;

    /// <summary>Nghỉ giữa hai tập (giây) để không dồn dập request tới TikTok.</summary>
    public int DelayBetweenVideosSeconds { get; set; } = 2;

    /// <summary>Một tập lỗi đủ số lần này thì thôi thử, tới khi người dùng bấm thử lại.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Nghỉ bao lâu (phút) khi TikTok chặn máy chủ.</summary>
    public int BlockedBackoffMinutes { get; set; } = 30;

    /// <summary>Kích thước file video tối đa (byte).</summary>
    public long MaxVideoBytes { get; set; } = 500L * 1024 * 1024;
}
