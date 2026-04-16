using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Cameras.Commands.CreateCamera;

public record CreateCameraCommand : IRequest<int>
{
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
}

public class CreateCameraCommandHandler : IRequestHandler<CreateCameraCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateCameraCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateCameraCommand request, CancellationToken cancellationToken)
    {
        var entity = new Camera
        {
            Title = request.Title,
            AreaId = request.AreaId,
            CameraType = request.CameraType,
            CameraLink = request.CameraLink,
            LanIpAddress = request.LanIpAddress,
            WabIpAddress = request.WabIpAddress,
            Frequency = request.Frequency,
            IntegratedCamId = request.IntegratedCamId,
            DeviceStatus = request.DeviceStatus,
            Brand = request.Brand,
            CameraUserName = request.CameraUserName,
            CameraPassword = request.CameraPassword,
            PtzType = request.PtzType,
            Onvif = request.Onvif,
            TourId = request.TourId,
            FireDetection = request.FireDetection,
            SmokeDetection = request.SmokeDetection,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };
        
        _context.Cameras.Add(entity);
        
        await _context.SaveChangesAsync(cancellationToken);
        
        return entity.Id;
    }
}
