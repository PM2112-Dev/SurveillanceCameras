namespace SurveillanceCameras.Domain.Common;

public abstract class BaseAuditableEntity : BaseEntity
{
    public BaseStatus BaseStatus { get; set; }

    public DateTimeOffset Created { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset LastModified { get; set; }

    public string? LastModifiedBy { get; set; }
}
