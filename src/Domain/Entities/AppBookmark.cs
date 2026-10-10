namespace SurveillanceCameras.Domain.Entities;

/// <summary>Phim người xem (app) đã lưu. Chủ là <c>CreatedBy</c>; mỗi người lưu một phim tối đa một lần.</summary>
public class AppBookmark : BaseAuditableEntity
{
    public int WorkId { get; set; }

    public Work? Work { get; set; }
}
