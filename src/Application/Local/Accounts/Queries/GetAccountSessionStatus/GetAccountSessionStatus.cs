using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.AccountSession;

namespace SurveillanceCameras.Application.Local.Accounts.Queries.GetAccountSessionStatus;

public record GetAccountSessionStatusQuery(int AccountId) : IRequest<AccountSessionStatus>;

public class GetAccountSessionStatusQueryValidator : AbstractValidator<GetAccountSessionStatusQuery>
{
    public GetAccountSessionStatusQueryValidator()
    {
    }
}

public class GetAccountSessionStatusQueryHandler : IRequestHandler<GetAccountSessionStatusQuery, AccountSessionStatus>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IAccountSessionManagerRepository _sessionManagerRepository;

    public GetAccountSessionStatusQueryHandler(
        IApplicationDbContext context, IUser user, IAccountSessionManagerRepository sessionManagerRepository)
    {
        _context = context;
        _user = user;
        _sessionManagerRepository = sessionManagerRepository;
    }

    public async Task<AccountSessionStatus> Handle(GetAccountSessionStatusQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Accounts
            .FirstOrDefaultAsync(x => x.Id == request.AccountId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.AccountId, entity);

        return _sessionManagerRepository.GetStatus(request.AccountId);
    }
}
