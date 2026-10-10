namespace SurveillanceCameras.Domain.Entities;

/// <summary>Bình luận của người xem (app) trên một phim. Chủ là <c>CreatedBy</c>.</summary>
public class AppComment : BaseAuditableEntity
{
    public int WorkId { get; set; }

    public string Content { get; set; } = "";

    /// <summary>Tên hiển thị của người viết tại lúc bình luận.</summary>
    public string? AuthorName { get; set; }

    public Work? Work { get; set; }
}
