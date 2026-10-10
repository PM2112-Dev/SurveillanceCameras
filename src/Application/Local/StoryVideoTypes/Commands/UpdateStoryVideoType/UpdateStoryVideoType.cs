using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.StoryVideoTypes.Commands.UpdateStoryVideoType;

public record UpdateStoryVideoTypeCommand : IRequest
{
    public int Id { get; init; }

    public string? Title { get; init; }

    public string? Code { get; init; }
}

public class UpdateStoryVideoTypeCommandValidator : AbstractValidator<UpdateStoryVideoTypeCommand>
{
    public UpdateStoryVideoTypeCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}

public class UpdateStoryVideoTypeCommandHandler : IRequestHandler<UpdateStoryVideoTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateStoryVideoTypeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateStoryVideoTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StoryVideoTypes
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Title = request.Title;
        entity.Code = request.Code;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
