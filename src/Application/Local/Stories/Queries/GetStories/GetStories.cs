using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Security;
using SurveillanceCameras.Application.Local.Stories.Queries.Model;

namespace SurveillanceCameras.Application.Stories.Queries.GetStories;

[Authorize]
public record GetStoriesQuery : IRequest<StoryVm>;

public class GetStoriesQueryHandler : IRequestHandler<GetStoriesQuery, StoryVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetStoriesQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<StoryVm> Handle(GetStoriesQuery request, CancellationToken cancellationToken)
    {
        return new StoryVm
        {
            Stories = await _context.Stories
                .AsNoTracking()
                .Where(story => story.CreatedBy ==  _user.Id)
                .ProjectTo<StoryDto>(_mapper.ConfigurationProvider)
                .OrderBy(x => x.Title)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken)
        };
    }
}
