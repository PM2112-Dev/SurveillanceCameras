namespace SurveillanceCameras.Domain.Entities;

/// <summary>
/// Tác phẩm: truyện hoặc phim (<see cref="Type"/>). Nội dung nằm ở <see cref="Episodes"/> — chương với truyện, tập với
/// phim. Tên tác phẩm là <c>Title</c>.
/// </summary>
public class Work : BaseAuditableEntity
{
    public WorkType Type { get; set; } = WorkType.Story;

    /// <summary>Nguồn truyện đã tạo ra truyện này. Phim không có (lấy từ kênh, xem <see cref="UserProfileId"/>).</summary>
    public int? StorySourceId { get; set; }

    /// <summary>Kênh nguồn của phim (vd. kênh TikTok có drama).</summary>
    public int? UserProfileId { get; set; }

    /// <summary>ID của phim trên nền tảng nguồn (vd. dramaID của TikTok) — khoá để đồng bộ lại không tạo trùng.</summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// ID video (<see cref="Episode.ExternalId"/>) của các tập người dùng đã xoá — đồng bộ lại từ TikTok bỏ qua các ID này
    /// để tập đã xoá không tự quay lại.
    /// </summary>
    public List<string> ExcludedEpisodeIds { get; set; } = [];

    public string? CoverUrl { get; set; }

    /// <summary>Ngôn ngữ của tác phẩm. Với phim đồng bộ từ TikTok: ngôn ngữ có nhiều tập nhất.</summary>
    public int? LanguageId { get; set; }

    public Language? Language { get; set; }

    /// <summary>Lượt xem trên nền tảng nguồn (vd. numWatched của drama TikTok), cập nhật mỗi lần đồng bộ.</summary>
    public long? ViewCount { get; set; }

    /// <summary>Số người xem trên app đang lưu phim này (chỉ tính theo phim, không theo tập).</summary>
    public int BookmarkCount { get; set; }

    /// <summary>Tài khoản đăng truyện (TYT). Truyện do AI tạo từ nguồn truyện chưa có, gắn sau khi sửa truyện.</summary>
    public int? AccountId { get; set; }

    public int? StoryVideoTypeId { get; set; }

    public StoryVideoType? StoryVideoType { get; set; }

    public string? TytStoryId { get; set; }

    /// <summary>Lỗi của lần tạo truyện trên TYT gần nhất (chưa đăng nhập, TYT từ chối…).</summary>
    public string? TytError { get; set; }

    /// <summary>Lần gần nhất cập nhật thông tin truyện (giới thiệu, trạng thái…) lên TYT thành công.</summary>
    public DateTimeOffset? TytUpdatedAt { get; set; }

    // ── Lịch đăng chương lên TYT ──────────────────────────────────────────────
    /// <summary>Khi nào đăng chương đã xử lý xong lên TYT (chỉ áp khi truyện đã có <see cref="TytStoryId"/>).</summary>
    public TytUploadMode TytUploadMode { get; set; } = TytUploadMode.Manual;

    /// <summary>Số chương đăng mỗi lượt (chế độ theo giờ/ngày).</summary>
    public int TytUploadBatchSize { get; set; } = 1;

    /// <summary>Chế độ theo giờ: cách nhau bao nhiêu giờ giữa hai lượt đăng.</summary>
    public int TytUploadEveryHours { get; set; } = 1;

    /// <summary>Chế độ theo ngày: giờ đăng mỗi ngày, dạng "HH:mm" (giờ máy chủ).</summary>
    public string? TytUploadDailyTime { get; set; }

    /// <summary>Lần gần nhất tác vụ khôi phục lỗi đưa truyện lên TYT chạy lại — để không thử dồn dập.</summary>
    public DateTimeOffset? TytRetryQueuedAt { get; set; }

    public string? Description { get; set; }

    public string? Author { get; set; }

    public bool IsFull { get; set; }

    public bool IsPublished { get; set; }

    public bool IsEarning { get; set; }

    public bool IsComment { get; set; }

    public int ChapterUploaded { get; set; }

    public int EarnCount { get; set; }

    public int EarnNow { get; set; }

    public int AdCount { get; set; }

    public int Paid { get; set; }

    // ── Quyền xem trên app (phim) ─────────────────────────────────────────────
    /// <summary>
    /// Phim có cấu hình quyền xem riêng (các khoảng tập bên dưới). Tắt thì dùng mặc định của hệ thống (mục <c>App</c>
    /// trong cấu hình: N tập đầu miễn phí, M tập kế mở bằng quảng cáo, còn lại VIP).
    /// </summary>
    public bool HasCustomAccess { get; set; }

    /// <summary>
    /// Phim chỉ dành cho VIP: người chưa có VIP không xem được tập nào (bỏ qua các khoảng tập bên dưới), và app gắn nhãn
    /// VIP lên phim. Độc lập với <see cref="HasCustomAccess"/>.
    /// </summary>
    public bool IsVipOnly { get; set; }

    /// <summary>Tập đầu của khoảng mở bằng quảng cáo; <c>null</c> là phim không có tập quảng cáo.</summary>
    public int? AdFromEpisode { get; set; }

    /// <summary>Tập cuối của khoảng quảng cáo; <c>null</c> là tới hết phim.</summary>
    public int? AdToEpisode { get; set; }

    /// <summary>Tập đầu của khoảng chỉ dành cho VIP; <c>null</c> là phim không có tập VIP.</summary>
    public int? VipFromEpisode { get; set; }

    /// <summary>Tập cuối của khoảng VIP; <c>null</c> là tới hết phim.</summary>
    public int? VipToEpisode { get; set; }

    public StorySource? StorySource { get; set; }

    public UserProfile? UserProfile { get; set; }

    /// <summary>Lần đồng bộ danh sách chương/tập gần nhất chạy hết.</summary>
    public DateTimeOffset? EpisodesSyncedAt { get; set; }

    /// <summary>Lỗi của lần đồng bộ danh sách chương/tập gần nhất (lỗi từng chương nằm ở <c>Episode.Error</c>).</summary>
    public string? EpisodeSyncError { get; set; }

    public ICollection<Episode>? Episodes { get; set; }
}
