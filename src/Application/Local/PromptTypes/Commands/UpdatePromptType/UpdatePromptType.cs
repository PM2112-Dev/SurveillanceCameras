using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.PromptTypes.Commands.UpdatePromptType;

public record UpdatePromptTypeCommand : IRequest
{
    public int Id { get; init; }
    public string? Title { get; init; }
}

public class UpdatePromptTypeCommandValidator : AbstractValidator<UpdatePromptTypeCommand>
{
    public UpdatePromptTypeCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");
    }
}

public class UpdatePromptTypeCommandHandler : IRequestHandler<UpdatePromptTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdatePromptTypeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdatePromptTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.PromptTypes
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id,
            cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        
        entity.Title = request.Title;
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
