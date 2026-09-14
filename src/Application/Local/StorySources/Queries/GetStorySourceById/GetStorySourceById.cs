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

    public GetStorySourceByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<StorySourceDto> Handle(GetStorySourceByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.StorySources
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        return _mapper.Map<StorySourceDto>(entity);
    }
}
