using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Service.TikTokSession;

namespace SurveillanceCameras.Application.Local.Accounts.Commands.StartTikTokSession;

public record StartTikTokSessionCommand(int AccountId) : IRequest;

public class StartTikTokSessionCommandValidator : AbstractValidator<StartTikTokSessionCommand>
{
    public StartTikTokSessionCommandValidator()
    {
    }
}

public class StartTikTokSessionCommandHandler : IRequestHandler<StartTikTokSessionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ITikTokSessionManager _sessionManager;

    public StartTikTokSessionCommandHandler(
        IApplicationDbContext context, IUser user, ITikTokSessionManager sessionManager)
    {
        _context = context;
        _user = user;
        _sessionManager = sessionManager;
    }

    public async Task Handle(StartTikTokSessionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Accounts
            .FirstOrDefaultAsync(x => x.Id == request.AccountId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.AccountId, entity);

        await _sessionManager.StartAsync(request.AccountId, cancellationToken);
    }
}
