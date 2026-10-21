namespace SurveillanceCameras.Infrastructure.BackgroundJobs;

public class MovieDailySyncOptions
{
    public const string SectionName = "MovieDailySync";

    /// <summary>Tắt thì không tự đồng bộ lại phim.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Giờ chạy mỗi ngày theo giờ máy chủ, dạng "HH:mm".</summary>
    public string TimeOfDay { get; set; } = "03:00";

    /// <summary>Khoảng cách tối thiểu giữa hai drama được đồng bộ (giây), do consumer Kafka giữ để không dồn dập request tới TikTok.</summary>
    public int DelayBetweenDramasSeconds { get; set; } = 3;
}
