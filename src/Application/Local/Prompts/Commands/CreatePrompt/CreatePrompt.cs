using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Prompts.Commands.CreatePrompt;

public record CreatePromptCommand : IRequest<int>
{
    public int PromptTypeId { get; init; }

    public string? Title { get; init; }

    public string? Content { get; init; }
}

public class CreatePromptCommandValidator : AbstractValidator<CreatePromptCommand>
{
    public CreatePromptCommandValidator()
    {
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
