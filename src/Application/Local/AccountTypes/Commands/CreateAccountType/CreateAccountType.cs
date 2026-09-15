using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.AccountTypes.Commands.CreateAccountType;

public record CreateAccountTypeCommand : IRequest<int>
{
    public string? Title { get; init; }
}

public class CreateAccountTypeCommandValidator : AbstractValidator<CreateAccountTypeCommand>
{
    public CreateAccountTypeCommandValidator()
    {
    }
}

public class CreateAccountTypeCommandHandler : IRequestHandler<CreateAccountTypeCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateAccountTypeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateAccountTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = new AccountType { Title = request.Title };
        
        _context.AccountTypes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
