using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Service.TikTokSession;

namespace SurveillanceCameras.Application.Local.Accounts.Queries.GetTikTokSessionStatus;

public record GetTikTokSessionStatusQuery(int AccountId) : IRequest<TikTokSessionStatus>;

public class GetTikTokSessionStatusQueryValidator : AbstractValidator<GetTikTokSessionStatusQuery>
{
    public GetTikTokSessionStatusQueryValidator()
    {
    }
}

public class GetTikTokSessionStatusQueryHandler : IRequestHandler<GetTikTokSessionStatusQuery, TikTokSessionStatus>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ITikTokSessionManager _sessionManager;

    public GetTikTokSessionStatusQueryHandler(
        IApplicationDbContext context, IUser user, ITikTokSessionManager sessionManager)
    {
        _context = context;
        _user = user;
        _sessionManager = sessionManager;
    }

    public async Task<TikTokSessionStatus> Handle(GetTikTokSessionStatusQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Accounts
            .FirstOrDefaultAsync(x => x.Id == request.AccountId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.AccountId, entity);

        return _sessionManager.GetStatus(request.AccountId);
    }
}
