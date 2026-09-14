using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Categories.Commands.DeleteCategory;

public record DeleteCategoryCommand(int Id) : IRequest;

public class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
    }
}

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteCategoryCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Categories
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        
        _context.Categories.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
