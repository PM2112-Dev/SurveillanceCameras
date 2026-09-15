namespace SurveillanceCameras.Domain.Entities;

public class Account : BaseAuditableEntity
{
    public int AccountTypeId { get; set; }
    
    public string? Cookie { get; set; }
}
