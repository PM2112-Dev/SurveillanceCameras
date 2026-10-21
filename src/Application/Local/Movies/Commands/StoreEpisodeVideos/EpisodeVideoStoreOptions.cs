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

    /// <summary>Bao lâu quét một lần xem còn tập nào chưa lên kho để đưa vào hàng đợi (giây).</summary>
    public int IntervalSeconds { get; set; } = 60;

    /// <summary>Số tập tối đa đưa vào hàng đợi mỗi lượt quét (mỗi người dùng).</summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>Tập đã vào hàng đợi chưa quá số phút này thì lượt quét sau chưa đưa lại (hàng đợi còn đang xử lý).</summary>
    public int RequeueAfterMinutes { get; set; } = 60;

    /// <summary>Tập lỗi đủ số lần thử (<see cref="MaxAttempts"/>) được mở lại thử thêm một lần sau số giờ này (0 là không tự mở lại).</summary>
    public int ExhaustedRetryHours { get; set; } = 12;

    /// <summary>Kho còn ít hơn dung lượng này thì chưa đưa tập nào của người dùng vào hàng đợi.</summary>
    public long MinFreeBytes { get; set; } = 50L * 1000 * 1000;

    /// <summary>Dung lượng ước tính một video khi người dùng chưa có tập nào đã lưu để lấy trung bình.</summary>
    public long EstimatedVideoBytes { get; set; } = 30L * 1000 * 1000;

    /// <summary>Nghỉ giữa hai tập (giây) để không dồn dập request tới TikTok.</summary>
    public int DelayBetweenVideosSeconds { get; set; } = 2;

    /// <summary>Một tập lỗi đủ số lần này thì thôi thử, tới khi người dùng bấm thử lại.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Nghỉ bao lâu (phút) khi TikTok chặn máy chủ.</summary>
    public int BlockedBackoffMinutes { get; set; } = 30;

    /// <summary>Kích thước file video tối đa (byte).</summary>
    public long MaxVideoBytes { get; set; } = 500L * 1024 * 1024;
}
