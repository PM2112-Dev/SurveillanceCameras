using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.StorySources.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.StorySources.Queries.GetStorySourceById;

public record GetStorySourceByIdQuery(int Id) : IRequest<StorySourceDto>;

public class GetStorySourceByIdQueryValidator : AbstractValidator<GetStorySourceByIdQuery>
{
    public GetStorySourceByIdQueryValidator()
    {
    }
}

public class GetStorySourceByIdQueryHandler : IRequestHandler<GetStorySourceByIdQuery, StorySourceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetStorySourceByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<StorySourceDto> Handle(GetStorySourceByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.StorySources
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        return _mapper.Map<StorySourceDto>(entity);
    }
}
