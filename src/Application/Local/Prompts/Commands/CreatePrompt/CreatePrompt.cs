using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Prompts.Commands.CreatePrompt;

public record CreatePromptCommand : IRequest<int>
{
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
        throw new NotImplementedException();
    }
}
