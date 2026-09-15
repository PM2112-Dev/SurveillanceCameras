using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Accounts.Commands.UpdateAccount;

public record UpdateAccountCommand : IRequest
{
    public int Id { get; init; }
    
    public string? Title { get; init; }
    
    public int AccountTypeId { get; init; }
    
    public string? Cookie { get; init; }
}

public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
    }
}

public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateAccountCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Accounts
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);
        
        entity.Title = request.Title;
        entity.AccountTypeId = request.AccountTypeId;
        entity.Cookie = request.Cookie;
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
