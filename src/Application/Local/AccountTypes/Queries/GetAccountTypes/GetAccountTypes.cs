using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.AccountTypes.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.AccountTypes.Queries.GetAccountTypes;

public record GetAccountTypesQuery : IRequest<PaginatedList<AccountTypeDto>>
{
    public string? Title { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public class GetAccountTypesQueryValidator : AbstractValidator<GetAccountTypesQuery>
{
    public GetAccountTypesQueryValidator()
    {
    }
}

public class GetAccountTypesQueryHandler : IRequestHandler<GetAccountTypesQuery, PaginatedList<AccountTypeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetAccountTypesQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<AccountTypeDto>> Handle(GetAccountTypesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.AccountTypes.Where(x => x.CreatedBy == _user.Id);

        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => x.Title != null && x.Title.Contains(request.Title));

        return await PaginatedList<AccountTypeDto>.CreateAsync(
            query.OrderByDescending(x => x.Created).ProjectTo<AccountTypeDto>(_mapper.ConfigurationProvider),
            request.Page, request.PageSize, cancellationToken);
    }
}
