namespace SurveillanceCameras.Domain.Entities;

/// <summary>
/// Tiến độ xem một phim của người xem (app): tập đang xem và vị trí trong tập. Chủ là <c>CreatedBy</c>; mỗi người một dòng
/// cho mỗi phim.
/// </summary>
public class AppWatchHistory : BaseAuditableEntity
{
    public int WorkId { get; set; }

    public int EpisodeNumber { get; set; }

    public int PositionSeconds { get; set; }

    /// <summary>Thời lượng tập đang xem (giây) — để tính phần trăm đã xem; 0 khi chưa biết.</summary>
    public int DurationSeconds { get; set; }

    public DateTimeOffset WatchedAt { get; set; }

    public Work? Work { get; set; }
}
