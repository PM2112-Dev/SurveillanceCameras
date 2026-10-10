using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.AccountSession;

namespace SurveillanceCameras.Application.Local.Accounts.Commands.StopAccountSession;

public record StopAccountSessionCommand(int AccountId) : IRequest;

public class StopAccountSessionCommandValidator : AbstractValidator<StopAccountSessionCommand>
{
    public StopAccountSessionCommandValidator()
    {
    }
}

public class StopAccountSessionCommandHandler : IRequestHandler<StopAccountSessionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IAccountSessionManagerRepository _sessionManagerRepository;

    public StopAccountSessionCommandHandler(
        IApplicationDbContext context, IUser user, IAccountSessionManagerRepository sessionManagerRepository)
    {
        _context = context;
        _user = user;
        _sessionManagerRepository = sessionManagerRepository;
    }

    public async Task Handle(StopAccountSessionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Accounts
            .FirstOrDefaultAsync(x => x.Id == request.AccountId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.AccountId, entity);

        await _sessionManagerRepository.StopAsync(request.AccountId);
    }
}
