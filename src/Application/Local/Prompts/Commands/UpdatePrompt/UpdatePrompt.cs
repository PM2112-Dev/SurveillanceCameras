using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Prompts.Commands.UpdatePrompt;

public record UpdatePromptCommand : IRequest
{
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

    public UpdatePromptCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdatePromptCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
