namespace SurveillanceCameras.Domain.Entities;

public class Prompt : BaseAuditableEntity
{
    public string? Type { get; set; }
    
    public string? Content { get; set; }
}
