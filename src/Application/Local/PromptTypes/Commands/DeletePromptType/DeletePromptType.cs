using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.PromptTypes.Commands.DeletePromptType;

public record DeletePromptTypeCommand(int Id) : IRequest
{
}

public class DeletePromptTypeCommandValidator : AbstractValidator<DeletePromptTypeCommand>
{
    public DeletePromptTypeCommandValidator()
    {
    }
}

public class DeletePromptTypeCommandHandler : IRequestHandler<DeletePromptTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeletePromptTypeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeletePromptTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.PromptTypes
            .FirstOrDefaultAsync(x=> x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        
        _context.PromptTypes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
