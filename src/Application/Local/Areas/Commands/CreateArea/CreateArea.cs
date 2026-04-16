using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Security;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Areas.Commands.CreateArea;

[Authorize]
public record CreateAreaCommand : IRequest<int>
{
    public string? Title { get; init; }

    public int? ParentAreaId { get; init; }
    
    public AreaType AreaType { get; init; }

    public MapType MapType { get; init; }

    public string? PhotoPath { get; init; }

    public double? Longitude { get; init; }

    public double? Latitude { get; init; }

    public int? Zoom { get; init; }

    public string? Note { get; init; }
}

public class CreateAreaCommandHandler : IRequestHandler<CreateAreaCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateAreaCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateAreaCommand request, CancellationToken cancellationToken)
    {
        var entity = new Area
        {
            Title = request.Title,
            ParentAreaId = request.ParentAreaId,
            AreaType = request.AreaType,
            MapType = request.MapType,
            PhotoPath = request.PhotoPath,
            Longitude = request.Longitude,
            Latitude = request.Latitude,
            Zoom = request.Zoom,
            Note = request.Note
        };

        _context.Areas.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
