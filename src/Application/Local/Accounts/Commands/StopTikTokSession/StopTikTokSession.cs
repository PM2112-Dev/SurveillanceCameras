using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Service.TikTokSession;

namespace SurveillanceCameras.Application.Local.Accounts.Commands.StopTikTokSession;

public record StopTikTokSessionCommand(int AccountId) : IRequest;

public class StopTikTokSessionCommandValidator : AbstractValidator<StopTikTokSessionCommand>
{
    public StopTikTokSessionCommandValidator()
    {
    }
}

public class StopTikTokSessionCommandHandler : IRequestHandler<StopTikTokSessionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ITikTokSessionManager _sessionManager;

    public StopTikTokSessionCommandHandler(
        IApplicationDbContext context, IUser user, ITikTokSessionManager sessionManager)
    {
        _context = context;
        _user = user;
        _sessionManager = sessionManager;
    }

    public async Task Handle(StopTikTokSessionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Accounts
            .FirstOrDefaultAsync(x => x.Id == request.AccountId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.AccountId, entity);

        await _sessionManager.StopAsync(request.AccountId);
    }
}
