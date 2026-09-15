using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Security;

namespace SurveillanceCameras.Application.Local.Stories.Commands.DeleteStory;

[Authorize]
public record DeleteStoryCommand(int Id) : IRequest;

public class DeleteStoryCommandValidator : AbstractValidator<DeleteStoryCommand>
{
    public DeleteStoryCommandValidator()
    {
    }
}

public class DeleteStoryCommandHandler : IRequestHandler<DeleteStoryCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteStoryCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteStoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Stories
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);
        
        _context.Stories.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
