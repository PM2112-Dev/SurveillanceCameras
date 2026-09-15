using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.AccountTypes.Commands.UpdateAccountType;

public record UpdateAccountTypeCommand : IRequest
{
    public int Id { get; init; }
    
    public string? Title { get; init; }
}

public class UpdateAccountTypeCommandValidator : AbstractValidator<UpdateAccountTypeCommand>
{
    public UpdateAccountTypeCommandValidator()
    {
    }
}

public class UpdateAccountTypeCommandHandler : IRequestHandler<UpdateAccountTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateAccountTypeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateAccountTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.AccountTypes
            .FirstOrDefaultAsync(x=> x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);
        
        entity.Title = request.Title;
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
