using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Prompts.Commands.DeletePrompt;

public record DeletePromptCommand : IRequest
{
}

public class DeletePromptCommandValidator : AbstractValidator<DeletePromptCommand>
{
    public DeletePromptCommandValidator()
    {
    }
}

public class DeletePromptCommandHandler : IRequestHandler<DeletePromptCommand>
{
    private readonly IApplicationDbContext _context;

    public DeletePromptCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeletePromptCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
