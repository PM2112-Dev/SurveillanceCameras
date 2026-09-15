namespace SurveillanceCameras.Domain.Entities;

public class AI : BaseAuditableEntity
{
    public string? Model { get; set; }
    
    public string? ApiKey { get; set; }
}
