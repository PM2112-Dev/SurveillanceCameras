using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.StoryVideoTypes.Commands.DeleteStoryVideoType;

public record DeleteStoryVideoTypeCommand(int Id) : IRequest;

public class DeleteStoryVideoTypeCommandValidator : AbstractValidator<DeleteStoryVideoTypeCommand>
{
    public DeleteStoryVideoTypeCommandValidator()
    {
    }
}

public class DeleteStoryVideoTypeCommandHandler : IRequestHandler<DeleteStoryVideoTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteStoryVideoTypeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteStoryVideoTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StoryVideoTypes
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        _context.StoryVideoTypes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
