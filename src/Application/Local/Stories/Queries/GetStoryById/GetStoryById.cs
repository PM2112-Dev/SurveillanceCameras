using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Stories.Queries.DTOs;
using SurveillanceCameras.Application.Local.Stories.Queries.GetStories;

namespace SurveillanceCameras.Application.Local.Stories.Queries.GetStoryById;

public record GetStoryByIdQuery(int Id) : IRequest<StoryDto>;

public class GetStoryByIdQueryValidator : AbstractValidator<GetStoriesQuery>
{
    public GetStoryByIdQueryValidator()
    {
    }
}

public class GetStoryByIdQueryHandler : IRequestHandler<GetStoryByIdQuery, StoryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public GetStoryByIdQueryHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public async Task<StoryDto> Handle(GetStoryByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Stories
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        return _mapper.Map<StoryDto>(entity);
    }
}
