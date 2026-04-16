using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Areas.Commands.UpdateArea;

public record UpdateAreaCommand : IRequest
{
    public int Id { get; set; }
    
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

public class UpdateAreaCommandHandler : IRequestHandler<UpdateAreaCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateAreaCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }   

    public async Task Handle(UpdateAreaCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Areas
            .FindAsync([request.Id], cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Title = request.Title;
        entity.ParentAreaId = request.ParentAreaId;
        entity.AreaType = request.AreaType;
        entity.MapType = request.MapType;
        entity.PhotoPath = request.PhotoPath;
        entity.Note = request.Note;
        entity.Longitude = request.Longitude;
        entity.Latitude = request.Latitude;
        entity.Zoom = request.Zoom;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
