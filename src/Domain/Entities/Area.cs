namespace SurveillanceCameras.Domain.Entities;

public class Area : BaseAuditableEntity
{
    public int? ParentAreaId { get; set; }
    
    public AreaType AreaType { get; set; }
    
    public MapType MapType { get; set; }
    
    public string? PhotoPath { get; set; }
    
    public double? Longitude { get; set; }
    
    public double? Latitude { get; set; }

    public int? Zoom { get; set; }

    public string? Note { get; set; }
}
