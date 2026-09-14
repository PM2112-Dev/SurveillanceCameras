namespace SurveillanceCameras.Domain.Common;

public abstract class BaseDto
{
    public int Id { get; set; }
    
    public string? Title { get; set; }
    
    public string? Code {get; init;}
    
    public string? BaseStatus { get; set; }
    
    // public DateTimeOffset Created { get; set; }
    //
    // public string? CreatedBy { get; set; }
    //
    // public DateTimeOffset LastModified { get; set; }
    //
    // public string? LastModifiedBy { get; set; }
}
