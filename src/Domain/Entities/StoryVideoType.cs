namespace SurveillanceCameras.Domain.Entities;

public class StoryVideoType : BaseAuditableEntity
{
    public ICollection<Work>? Works { get; init; }
}
