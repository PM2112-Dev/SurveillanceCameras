namespace SurveillanceCameras.Domain.Entities;

public class Prompt : BaseAuditableEntity
{
    public int PromptTypeId { get; set; }
    
    public string? Content { get; set; }
}
