using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repositories;

namespace SurveillanceCameras.Application.Local.Categories.Commands.UpdateCategory;

public record UpdateCategoryCommand : IRequest
{
    public int Id { get; init; }

    public string? Title { get; init; }

    public IReadOnlyCollection<int> StorySourceIds { get; init; } = new List<int>();
}

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(500);
    }
}

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateCategoryCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Categories
            .Include(x => x.StorySources)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Title = request.Title;

        entity.StorySources = request.StorySourceIds.Count > 0
            ? await _context.StorySources
                .Where(x => request.StorySourceIds.Contains(x.Id))
                .ToListAsync(cancellationToken)
            : [];

        await _context.SaveChangesAsync(cancellationToken);
    }
}
