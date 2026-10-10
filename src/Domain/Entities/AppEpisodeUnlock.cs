namespace SurveillanceCameras.Domain.Entities;

/// <summary>Tập người xem (app) đã mở bằng quảng cáo. Chủ là <c>CreatedBy</c>.</summary>
public class AppEpisodeUnlock : BaseAuditableEntity
{
    public int EpisodeId { get; set; }

    public Episode? Episode { get; set; }
}
