namespace SurveillanceCameras.Domain.Entities;

public class WebSource : BaseAuditableEntity
{
    public string? BaseUrl { get; set; }
    
    public ICollection<StorySource>? StorySources { get; set; }
}
