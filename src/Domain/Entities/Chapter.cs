namespace SurveillanceCameras.Domain.Entities;

public class Chapter : BaseAuditableEntity
{
    public int StoryId { get; set; }
    
    public string? ChapterTytId { get; set; }
    
    public string? NameRaw { get; set; }
    
    public string? NameEdit { get; set; }
    
    public int ChapterNumber { get; set; }
    
    public string? ContentRaw { get; set; }
    
    public string? ContentEdit { get; set; }
    
    public bool IsUploaded { get; set; }
    
    public string? Description { get; set; }
}
