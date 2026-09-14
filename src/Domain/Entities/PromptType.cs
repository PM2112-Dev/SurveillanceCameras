namespace SurveillanceCameras.Domain.Entities;

public class PromptType : BaseAuditableEntity
{
    public ICollection<Prompt>? Prompts { get; init; }
}
