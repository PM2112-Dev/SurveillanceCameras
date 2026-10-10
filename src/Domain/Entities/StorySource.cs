namespace SurveillanceCameras.Domain.Entities;

public class StorySource : BaseAuditableEntity
{
    public int WebSourceId { get; set; }
    public short StorySourceType { get; set; }
    public string? SinoVietnamese { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? LinkRaw { get; set; }
    public string? Status { get; set; }
    public string? LastUpdate { get; set; }
    public string? ImageUrl { get; set; }
    public int? TotalChapters { get; set; }

    /// <summary>
    /// Lỗi của lần gần nhất dùng AI tạo truyện (Story) từ nguồn này; <c>null</c> khi chưa chạy hoặc đã tạo thành công.
    /// </summary>
    public string? StoryCreationError { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();

    /// <summary>Truyện đã tạo từ nguồn này (thường tối đa một).</summary>
    public ICollection<Work>? Works { get; set; }
}
