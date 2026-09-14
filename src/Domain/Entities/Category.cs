namespace SurveillanceCameras.Domain.Entities;

public class Category : BaseAuditableEntity
{
    public ICollection<StorySource> StorySources { get; set; } = new List<StorySource>();
}
