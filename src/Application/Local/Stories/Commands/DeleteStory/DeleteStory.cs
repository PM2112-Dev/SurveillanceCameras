using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Stories.Commands.DeleteStory;

public record DeleteStoryCommand : IRequest
{
}

public class DeleteStoryCommandValidator : AbstractValidator<DeleteStoryCommand>
{
    public DeleteStoryCommandValidator()
    {
    }
}

public class DeleteStoryCommandHandler : IRequestHandler<DeleteStoryCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteStoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteStoryCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
