using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Cameras.Commands.DeleteCamera;

public record DeleteCameraCommand(int Id) : IRequest;

public class DeleteCameraCommandHandler : IRequestHandler<DeleteCameraCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteCameraCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteCameraCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Cameras
            .FindAsync([request.Id], cancellationToken);
        
        Guard.Against.Null(entity, nameof(DeleteCameraCommand));
        
        _context.Cameras.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
