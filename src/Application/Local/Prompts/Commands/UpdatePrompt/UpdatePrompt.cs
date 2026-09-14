using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Prompts.Commands.UpdatePrompt;

public record UpdatePromptCommand : IRequest
{
    public int Id { get; init; }

    public int PromptTypeId { get; init; }

    public string? Title { get; init; }

    public string? Content { get; init; }
}

public class UpdatePromptCommandValidator : AbstractValidator<UpdatePromptCommand>
{
    public UpdatePromptCommandValidator()
    {
    }
}

public class UpdatePromptCommandHandler : IRequestHandler<UpdatePromptCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdatePromptCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdatePromptCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Prompts
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Title = request.Title;
        entity.PromptTypeId = request.PromptTypeId;
        entity.Content = request.Content;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
