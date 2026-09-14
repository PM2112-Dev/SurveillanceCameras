using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.StorySources.Commands.DeleteStorySource;

public record DeleteStorySourceCommand(int Id) : IRequest;

public class DeleteStorySourceCommandValidator : AbstractValidator<DeleteStorySourceCommand>
{
    public DeleteStorySourceCommandValidator()
    {
    }
}

public class DeleteStorySourceCommandHandler : IRequestHandler<DeleteStorySourceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteStorySourceCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteStorySourceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StorySources
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id,
            cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        
        _context.StorySources.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
