namespace SurveillanceCameras.Domain.Entities;

public class StorySource : BaseAuditableEntity
{
    public int WebSourceId { get; set; }
    public short StorySourceType { get; set; }
    public string? SinoVietnamese  { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? LinkRaw { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
    public string? ImageUrl { get; set; }
    public int? TotalChapters { get; set; }
    
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
