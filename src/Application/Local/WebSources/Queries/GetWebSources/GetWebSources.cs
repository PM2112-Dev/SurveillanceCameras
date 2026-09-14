using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.WebSources.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.WebSources.Queries.GetWebSources;

public record GetWebSourcesQuery : IRequest<PaginatedList<WebSourceDto>>
{
    public string? Title { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetWebSourcesQueryValidator : AbstractValidator<GetWebSourcesQuery>
{
    public GetWebSourcesQueryValidator()
    {
    }
}

public class GetWebSourcesQueryHandler : IRequestHandler<GetWebSourcesQuery, PaginatedList<WebSourceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetWebSourcesQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<WebSourceDto>> Handle(GetWebSourcesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.WebSources.Where(x => x.CreatedBy == _user.Id);

        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => x.Title != null && x.Title.Contains(request.Title));

        return await PaginatedList<WebSourceDto>.CreateAsync(
            query.OrderByDescending(x => x.Created).ProjectTo<WebSourceDto>(_mapper.ConfigurationProvider),
            request.Page, request.PageSize, cancellationToken);
    }
}
