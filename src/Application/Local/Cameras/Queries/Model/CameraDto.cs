using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Cameras.Queries.Model;

public class CameraDto
{
    public int Id { get; init; }
    
    public string? Title { get; init; }
    
    public int AreaId { get; init; }
    
    public short? CameraType { get; init; }
    
    public string? CameraLink { get; init; }
    
    public string? LanIpAddress { get; init; }
    
    public string? WabIpAddress { get; init; }
    
    public int Frequency { get; init; }
    
    public int? IntegratedCamId { get; init; }
    
    public short DeviceStatus { get; init; }
    
    public short Brand { get; init; }
    
    public string? CameraUserName { get; init; }
    
    public string? CameraPassword { get; init; }
    
    public short PtzType { get; init; }
    
    public bool Onvif { get; init; }
    
    public int TourId { get; init; }
    
    public bool FireDetection { get; init; }
    
    public bool SmokeDetection { get; init; }
    
    public double? Latitude { get; init; }
    
    public double? Longitude { get; init; }
    
     private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Camera, CameraDto>();
        }
    }
}
