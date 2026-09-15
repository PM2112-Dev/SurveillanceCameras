using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.AccountTypes.Commands.DeleteAccountType;

public record DeleteAccountTypeCommand(int Id) : IRequest;

public class DeleteAccountTypeCommandValidator : AbstractValidator<DeleteAccountTypeCommand>
{
    public DeleteAccountTypeCommandValidator()
    {
    }
}

public class DeleteAccountTypeCommandHandler : IRequestHandler<DeleteAccountTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteAccountTypeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteAccountTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.AccountTypes
            .FirstOrDefaultAsync(x=>x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        
        _context.AccountTypes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
