namespace SurveillanceCameras.Domain.Entities;

public class Account : BaseAuditableEntity
{
    public AccountType AccountType { get; set; }
    
    public string? Cookie { get; set; }
}
