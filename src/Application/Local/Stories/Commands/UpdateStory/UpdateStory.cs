using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Stories.Commands.UpdateStory;

public record UpdateStoryCommand : IRequest
{
}

public class UpdateStoryCommandValidator : AbstractValidator<UpdateStoryCommand>
{
    public UpdateStoryCommandValidator()
    {
    }
}

public class UpdateStoryCommandHandler : IRequestHandler<UpdateStoryCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateStoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateStoryCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
