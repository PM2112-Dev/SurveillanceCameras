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

    public string? CoverUrl { get; set; }

    /// <summary>Ngôn ngữ của tác phẩm. Với phim đồng bộ từ TikTok: ngôn ngữ có nhiều tập nhất.</summary>
    public int? LanguageId { get; set; }

    public Language? Language { get; set; }

    /// <summary>Lượt xem trên nền tảng nguồn (vd. numWatched của drama TikTok), cập nhật mỗi lần đồng bộ.</summary>
    public long? ViewCount { get; set; }

    /// <summary>Tài khoản đăng truyện (TYT). Truyện do AI tạo từ nguồn truyện chưa có, gắn sau khi sửa truyện.</summary>
    public int? AccountId { get; set; }

    public int? StoryVideoTypeId { get; set; }

    public StoryVideoType? StoryVideoType { get; set; }

    public string? TytStoryId { get; set; }

    /// <summary>Lỗi của lần tạo truyện trên TYT gần nhất (chưa đăng nhập, TYT từ chối…).</summary>
    public string? TytError { get; set; }

    /// <summary>Lần gần nhất cập nhật thông tin truyện (giới thiệu, trạng thái…) lên TYT thành công.</summary>
    public DateTimeOffset? TytUpdatedAt { get; set; }

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
