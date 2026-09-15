using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Accounts.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.Accounts.Queries.GetAccounts;

public record GetAccountsQuery : IRequest<PaginatedList<AccountDto>>
{
    public string? Title { get; init; }
    public int? AccountTypeId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetAccountsQueryValidator : AbstractValidator<GetAccountsQuery>
{
    public GetAccountsQueryValidator()
    {
    }
}

public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, PaginatedList<AccountDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetAccountsQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<AccountDto>> Handle(GetAccountsQuery request, 
        CancellationToken cancellationToken)
    {
        var query = _context.Accounts.Where(x => x.CreatedBy == _user.Id);

        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => x.Title != null && x.Title.Contains(request.Title));

        if (request.AccountTypeId.HasValue)
            query = query.Where(x => x.AccountTypeId == request.AccountTypeId);
        
        return await PaginatedList<AccountDto>.CreateAsync(
            query.OrderByDescending(x => x.Created).ProjectTo<AccountDto>(_mapper.ConfigurationProvider),
            request.Page, request.PageSize, cancellationToken);
    }
}
