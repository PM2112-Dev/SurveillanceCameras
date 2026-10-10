using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Security;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Prompts.Commands.CreatePrompt;

[Authorize]
public record CreatePromptCommand : IRequest<int>
{
    public int PromptTypeId { get; init; }

    public string? Title { get; init; }

    public string? Content { get; init; }
}

public class CreatePromptCommandValidator : AbstractValidator<CreatePromptCommand>
{
    public CreatePromptCommandValidator(IApplicationDbContext context, IUser user)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.PromptTypeId)
            .MustAsync((id, ct) => PromptValidation.PromptTypeExistsAsync(context, user, id, ct))
            .WithMessage("Chọn loại prompt hợp lệ.");
    }
}

public class CreatePromptCommandHandler : IRequestHandler<CreatePromptCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreatePromptCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreatePromptCommand request, CancellationToken cancellationToken)
    {
        var entity = new Prompt
        {
            PromptTypeId = request.PromptTypeId, Title = request.Title, Content = request.Content
        };
        
        _context.Prompts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
