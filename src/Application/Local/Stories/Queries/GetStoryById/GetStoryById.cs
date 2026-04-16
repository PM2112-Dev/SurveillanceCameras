using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Stories.Queries.Model;

namespace SurveillanceCameras.Application.Stories.Queries.GetStoryById;

public record GetStoryByIdQuery(int Id) : IRequest<StoryDto>;

public class GetStoryByIdQueryHandler : IRequestHandler<GetStoryByIdQuery, StoryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetStoryByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<StoryDto> Handle(GetStoryByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Stories
            .AsNoTracking()
            .Where(story => story.Id == request.Id & story.CreatedBy ==  _user.Id);
        
        var entity = await query
            .ProjectTo<StoryDto>(_mapper.ConfigurationProvider)
            .SingleOrDefaultAsync(cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);
        
        return entity;
    }
}
