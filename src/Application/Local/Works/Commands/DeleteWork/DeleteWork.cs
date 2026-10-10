using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Security;

namespace SurveillanceCameras.Application.Local.Works.Commands.DeleteWork;

[Authorize]
public record DeleteWorkCommand(int Id) : IRequest;

public class DeleteWorkCommandValidator : AbstractValidator<DeleteWorkCommand>
{
    public DeleteWorkCommandValidator()
    {
    }
}

public class DeleteWorkCommandHandler : IRequestHandler<DeleteWorkCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteWorkCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteWorkCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Works
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);
        
        _context.Works.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
