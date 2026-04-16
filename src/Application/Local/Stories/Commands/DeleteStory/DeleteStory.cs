using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Stories.Commands.DeleteStory;

public record DeleteStoryCommand(int Id) : IRequest;

public class DeleteStoryCommandHandler : IRequestHandler<DeleteStoryCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteStoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteStoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Stories
            .FindAsync([request.Id], cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        _context.Stories.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
