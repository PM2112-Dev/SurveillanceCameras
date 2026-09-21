using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Service.TikTokApi;
using SurveillanceCameras.Application.TikTokApi;

namespace SurveillanceCameras.Application.Local.Accounts.Queries.GetTikTokDramaList;

public record GetTikTokDramaListQuery(int AccountId, string SecUid, int Count = 20, string Cursor = "0")
    : IRequest<DramaListDto>;

public class GetTikTokDramaListQueryValidator : AbstractValidator<GetTikTokDramaListQuery>
{
    public GetTikTokDramaListQueryValidator()
    {
        RuleFor(x => x.SecUid).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 50);
        RuleFor(x => x.Cursor).NotEmpty();
    }
}

public class GetTikTokDramaListQueryHandler : IRequestHandler<GetTikTokDramaListQuery, DramaListDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ITikTokApiService _tikTokApi;

    public GetTikTokDramaListQueryHandler(
        IApplicationDbContext context, IUser user, ITikTokApiService tikTokApi)
    {
        _context = context;
        _user = user;
        _tikTokApi = tikTokApi;
    }

    public async Task<DramaListDto> Handle(GetTikTokDramaListQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.AccountId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.AccountId, entity);

        return await _tikTokApi.GetDramaListAsync(
            entity.Cookie, request.SecUid, request.Count, request.Cursor, cancellationToken);
    }
}
