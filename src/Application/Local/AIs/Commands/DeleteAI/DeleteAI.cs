using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.AIs.Commands.DeleteAI;

public record DeleteAICommand(int Id) : IRequest;

public class DeleteAICommandValidator : AbstractValidator<DeleteAICommand>
{
    public DeleteAICommandValidator()
    {
    }
}

public class DeleteAICommandHandler : IRequestHandler<DeleteAICommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteAICommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteAICommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.AIs
            .FirstOrDefaultAsync(x=> x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);
        
        _context.AIs.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
