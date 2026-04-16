using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Cameras.Commands.UpdateCamera;

public record UpdateCameraCommand : IRequest
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
}

public class UpdateCameraCommandHandler : IRequestHandler<UpdateCameraCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateCameraCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateCameraCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Cameras
            .FindAsync([request.Id], cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);
        
        entity.Title = request.Title;
        entity.AreaId = request.AreaId;
        entity.CameraType = request.CameraType;
        entity.CameraLink = request.CameraLink;
        entity.LanIpAddress = request.LanIpAddress;
        entity.WabIpAddress = request.WabIpAddress;
        entity.Frequency = request.Frequency;
        entity.IntegratedCamId = request.IntegratedCamId;
        entity.DeviceStatus = request.DeviceStatus;
        entity.Brand = request.Brand;
        entity.CameraUserName = request.CameraUserName;
        entity.CameraPassword = request.CameraPassword;
        entity.PtzType = request.PtzType;
        entity.Onvif = request.Onvif;
        entity.TourId = request.TourId;
        entity.FireDetection = request.FireDetection;
        entity.SmokeDetection = request.SmokeDetection;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
