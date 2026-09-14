using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Categories.Commands.CreateCategory;

public record CreateCategoryCommand : IRequest<int>
{
    public string? Title { get; init; }
}

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
    }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = new Category
        {
            Title = request.Title
        };

        _context.Categories.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
