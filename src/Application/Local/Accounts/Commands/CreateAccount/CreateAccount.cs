using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Accounts.Commands.CreateAccount;

public record CreateAccountCommand : IRequest<int>
{
    public int AccountTypeId { get; init; }

    public string? Title { get; init; }

    public string? Cookie { get; init; }
}

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
    }
}

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateAccountCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var entity = new Account
        {
            Title = request.Title, AccountTypeId = request.AccountTypeId, Cookie = request.Cookie
        };
        
        _context.Accounts.Add(entity);
        
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
