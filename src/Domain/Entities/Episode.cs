namespace SurveillanceCameras.Domain.Entities;

/// <summary>Chương của truyện hoặc tập của phim (xem <see cref="Work.Type"/>).</summary>
public class Episode : BaseAuditableEntity
{
    public int WorkId { get; set; }

    /// <summary>Prompt đã dùng để AI biên tập nội dung; <c>null</c> khi chưa biên tập.</summary>
    public int? PromptId { get; set; }

    public string? TytChapterId { get; set; }

    /// <summary>Link chương trên trang nguồn — khoá để nhận ra chương đã có khi đồng bộ lại mục lục.</summary>
    public string? LinkRaw { get; set; }

    public string? NameRaw { get; set; }

    public string? NameEdit { get; set; }

    public int EpisodeNumber { get; set; }

    public string? ContentRaw { get; set; }

    public string? ContentEdit { get; set; }

    public EpisodeStatus Status { get; set; } = EpisodeStatus.Listed;

    /// <summary>Lỗi của bước gần nhất (tải nội dung hoặc AI biên tập).</summary>
    public string? Error { get; set; }

    public DateTimeOffset? FetchedAt { get; set; }

    public DateTimeOffset? EditedAt { get; set; }

    // ── Tập phim ──────────────────────────────────────────────────────────────

    /// <summary>ID của tập trên nền tảng nguồn (vd. id video TikTok).</summary>
    public string? ExternalId { get; set; }

    /// <summary>Link video gốc của tập phim.</summary>
    public string? VideoUrl { get; set; }

    /// <summary>
    /// Link file video phát trực tiếp được (mp4/HLS, https) do hệ thống tự lưu trữ. Có thì app phát bằng link này và cho
    /// tải về; không có thì app phát qua trình phát nhúng của nền tảng nguồn (theo <see cref="ExternalId"/>).
    /// </summary>
    public string? StreamUrl { get; set; }

    /// <summary>Tài khoản lưu trữ đang giữ file video của tập; <c>null</c> khi chưa lưu hoặc link do người dùng tự gán.</summary>
    public int? StorageAccountId { get; set; }

    /// <summary>Khoá (đường dẫn) của file video trong bucket.</summary>
    public string? StorageKey { get; set; }

    public long? StorageSizeBytes { get; set; }

    /// <summary>Lỗi của lần lưu video gần nhất; <c>null</c> khi chưa thử hoặc đã lưu xong.</summary>
    public string? StorageError { get; set; }

    /// <summary>Số lần lưu video đã thất bại — đủ số lần thì tác vụ nền thôi thử cho tới khi người dùng bấm thử lại.</summary>
    public int StorageAttempts { get; set; }

    public string? ThumbnailUrl { get; set; }

    /// <summary>Thời lượng tập phim (giây).</summary>
    public int? DurationSeconds { get; set; }

    /// <summary>Ngôn ngữ của tập (với tập phim TikTok: theo <c>textLanguage</c> của video).</summary>
    public int? LanguageId { get; set; }

    public Language? Language { get; set; }

    public bool IsUploaded { get; set; }

    public bool IsPublished { get; set; }

    public Prompt? Prompt { get; set; }

    public Work? Work { get; set; }

    public StorageAccount? StorageAccount { get; set; }
}
