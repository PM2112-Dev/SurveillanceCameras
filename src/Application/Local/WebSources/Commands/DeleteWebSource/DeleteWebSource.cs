using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.WebSources.Commands.DeleteWebSource;

public record DeleteWebSourceCommand(int Id) : IRequest;

public class DeleteWebSourceCommandValidator : AbstractValidator<DeleteWebSourceCommand>
{
    public DeleteWebSourceCommandValidator()
    {
    }
}

public class DeleteWebSourceCommandHandler : IRequestHandler<DeleteWebSourceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteWebSourceCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteWebSourceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.WebSources.Where(w => w.Id == request.Id && w.CreatedBy == _user.Id)
            .SingleOrDefaultAsync(cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);
        
        _context.WebSources.Remove(entity);
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
