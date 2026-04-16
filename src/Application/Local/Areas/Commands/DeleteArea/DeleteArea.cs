using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Areas.Commands.DeleteArea;

public record DeleteAreaCommand(int Id) : IRequest;

public class DeleteAreaCommandHandler : IRequestHandler<DeleteAreaCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteAreaCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteAreaCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Areas
            .FindAsync([request.Id], cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        _context.Areas.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
