using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models.TikTok;
using SurveillanceCameras.Application.Repository.TikTok;

namespace SurveillanceCameras.Application.Local.TikTok.Queries.GetTiktokDramaList;

/// <summary>Lấy danh sách drama của kênh <see cref="SecUid"/> bằng cookie của Account <see cref="AccountId"/>.</summary>
public record GetTiktokDramaListQuery : IRequest<DramaListDto>
{
    public int AccountId { get; init; }

    public required string SecUid { get; init; }

    public int Count { get; init; } = 20;

    public string Cursor { get; init; } = "0";
}

public class GetTiktokDramaListQueryValidator : AbstractValidator<GetTiktokDramaListQuery>
{
    public GetTiktokDramaListQueryValidator()
    {
        RuleFor(x => x.SecUid).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 50);
        RuleFor(x => x.Cursor).NotEmpty();
    }
}

public class GetTiktokDramaListQueryHandler : IRequestHandler<GetTiktokDramaListQuery, DramaListDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ITikTokRepository _repository;

    public GetTiktokDramaListQueryHandler(IApplicationDbContext context, IUser user, ITikTokRepository repository)
    {
        _context = context;
        _user = user;
        _repository = repository;
    }

    public async Task<DramaListDto> Handle(GetTiktokDramaListQuery request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.AccountId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.AccountId, account);

        return await _repository.FetchDramaList(
            account.Cookie, request.SecUid, request.Count, request.Cursor, cancellationToken);
    }
}
