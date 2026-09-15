using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Stories.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.Stories.Queries.GetStories;

public record GetStoriesQuery : IRequest<PaginatedList<StoryDto>>
{
    public string? Title { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetStoriesQueryValidator : AbstractValidator<GetStoriesQuery>
{
    public GetStoriesQueryValidator()
    {
    }
}

public class GetStoriesQueryHandler : IRequestHandler<GetStoriesQuery, PaginatedList<StoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public GetStoriesQueryHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public async Task<PaginatedList<StoryDto>> Handle(GetStoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Stories
            .Where(x=> x.CreatedBy == _user.Id);
        
        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => x.Title != null && x.Title.Contains(request.Title));

        return await PaginatedList<StoryDto>.CreateAsync(
            query.OrderByDescending(x => x.Created).ProjectTo<StoryDto>(_mapper.ConfigurationProvider),
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
