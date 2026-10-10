using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.StoryVideoTypes.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.StoryVideoTypes.Queries.GetStoryVideoTypeById;

public record GetStoryVideoTypeByIdQuery(int Id) : IRequest<StoryVideoTypeDto>;

public class GetStoryVideoTypeByIdQueryValidator : AbstractValidator<GetStoryVideoTypeByIdQuery>
{
    public GetStoryVideoTypeByIdQueryValidator()
    {
    }
}

public class GetStoryVideoTypeByIdQueryHandler : IRequestHandler<GetStoryVideoTypeByIdQuery, StoryVideoTypeDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetStoryVideoTypeByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<StoryVideoTypeDto> Handle(GetStoryVideoTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.StoryVideoTypes
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        return _mapper.Map<StoryVideoTypeDto>(entity);
    }
}
