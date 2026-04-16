namespace SurveillanceCameras.Domain.Entities;

public class Camera : BaseAuditableEntity
{
    public int AreaId { get; set; }
    
    public short? CameraType { get; set; }
    
    public string? CameraLink { get; set; }
    
    public string? LanIpAddress { get; set; }
    
    public string? WabIpAddress { get; set; }
    
    public int Frequency { get; set; }
    
    public int? IntegratedCamId { get; set; }
    
    public short DeviceStatus {get; set;}
    
    public short Brand { get; set; }
    
    public string? CameraUserName { get; set; }
    
    public string? CameraPassword { get; set; }
    
    public short PtzType { get; set; }
    
    public bool Onvif { get; set; }
    
    public int TourId { get; set; }
    
    public bool FireDetection { get; set; }
    
    public bool SmokeDetection { get; set; }
    
    public double? Latitude { get; set; }
    
    public double? Longitude { get; set; }

}
