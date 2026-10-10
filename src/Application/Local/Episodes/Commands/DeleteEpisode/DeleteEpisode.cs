using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Episodes.Commands.DeleteEpisode;

public record DeleteEpisodeCommand(int Id) : IRequest;

public class DeleteEpisodeCommandHandler : IRequestHandler<DeleteEpisodeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteEpisodeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteEpisodeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Episodes
            .FirstOrDefaultAsync(x=> x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        
        _context.Episodes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
