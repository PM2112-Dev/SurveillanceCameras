using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Stories.Commands.CreateStory;

public record CreateStoryCommand : IRequest<int>
{
}

public class CreateStoryCommandValidator : AbstractValidator<CreateStoryCommand>
{
    public CreateStoryCommandValidator()
    {
    }
}

public class CreateStoryCommandHandler : IRequestHandler<CreateStoryCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateStoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateStoryCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
