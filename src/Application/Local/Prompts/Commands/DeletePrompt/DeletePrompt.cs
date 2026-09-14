using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Security;

namespace SurveillanceCameras.Application.Local.Prompts.Commands.DeletePrompt;

[Authorize]
public record DeletePromptCommand(int Id) : IRequest;

public class DeletePromptCommandHandler : IRequestHandler<DeletePromptCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeletePromptCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeletePromptCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Prompts
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);
        
        _context.Prompts.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
