using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Common.Security;
using SurveillanceCameras.Application.Local.StorySources.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.StorySources.Queries.GetStorySources;

[Authorize]
public record GetStorySourcesQuery : IRequest<PaginatedList<StorySourceDto>>
{
    public int? WebSourceId { get; init; }
    public string? Title { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetStorySourcesQueryValidator : AbstractValidator<GetStorySourcesQuery>
{
    public GetStorySourcesQueryValidator()
    {
    }
}

public class GetStorySourcesQueryHandler : IRequestHandler<GetStorySourcesQuery, PaginatedList<StorySourceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetStorySourcesQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<StorySourceDto>> Handle(GetStorySourcesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.StorySources.Where(x => x.CreatedBy == _user.Id);

        if (request.WebSourceId is not null)
            query = query.Where(x => x.WebSourceId == request.WebSourceId);

        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => x.Title != null && x.Title.Contains(request.Title));

        return await PaginatedList<StorySourceDto>.CreateAsync(
            query.OrderByDescending(x => x.Created)
                .ProjectTo<StorySourceDto>(_mapper.ConfigurationProvider),
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
