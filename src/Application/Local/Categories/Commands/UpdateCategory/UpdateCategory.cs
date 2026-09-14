using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Categories.Commands.UpdateCategory;

public record UpdateCategoryCommand : IRequest
{
    public int Id { get; init; }

    public string? Title { get; init; }
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
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Title = request.Title;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
