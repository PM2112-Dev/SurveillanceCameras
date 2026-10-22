namespace SurveillanceCameras.Infrastructure.BackgroundJobs;

/// <summary>Đăng chương lên TYT theo lịch (mục <c>TytUpload</c> trong appsettings).</summary>
public class TytUploadOptions
{
    public const string SectionName = "TytUpload";

    /// <summary>Tắt thì không tự đăng chương theo lịch (người dùng vẫn bấm đăng tay được).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Bao lâu kiểm tra một lần xem truyện nào tới lượt đăng chương.</summary>
    public int ScheduleIntervalSeconds { get; set; } = 60;

    /// <summary>Khoảng cách tối thiểu giữa hai chương đăng liên tiếp lên TYT.</summary>
    public int DelayBetweenChaptersSeconds { get; set; } = 8;

    /// <summary>Số chương tối đa mỗi truyện được đưa vào hàng đợi mỗi lượt quét ở chế độ "đăng ngay".</summary>
    public int ImmediateBatchSize { get; set; } = 20;

    /// <summary>Bao lâu kiểm tra một lần xem TYT đã duyệt các truyện đang chờ chưa, và đồng bộ thu nhập (phút). Mặc định 720 = 2 lần mỗi ngày.</summary>
    public int ApprovalCheckIntervalMinutes { get; set; } = 720;

    /// <summary>Múi giờ để hiểu giờ đăng mỗi ngày của truyện (IANA, vd. Asia/Ho_Chi_Minh).</summary>
    public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";
}
