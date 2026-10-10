using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.StoryVideoTypes.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.StoryVideoTypes.Queries.GetStoryVideoTypes;

public record GetStoryVideoTypesQuery : IRequest<PaginatedList<StoryVideoTypeDto>>
{
    public string? Title { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public class GetStoryVideoTypesQueryValidator : AbstractValidator<GetStoryVideoTypesQuery>
{
    public GetStoryVideoTypesQueryValidator()
    {
    }
}

public class GetStoryVideoTypesQueryHandler : IRequestHandler<GetStoryVideoTypesQuery, PaginatedList<StoryVideoTypeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetStoryVideoTypesQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<StoryVideoTypeDto>> Handle(GetStoryVideoTypesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.StoryVideoTypes.Where(x => x.CreatedBy == _user.Id);

        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => x.Title != null && x.Title.Contains(request.Title));

        return await PaginatedList<StoryVideoTypeDto>.CreateAsync(
            query.OrderByDescending(x => x.Created).ProjectTo<StoryVideoTypeDto>(_mapper.ConfigurationProvider),
            request.Page, request.PageSize, cancellationToken);
    }
}
