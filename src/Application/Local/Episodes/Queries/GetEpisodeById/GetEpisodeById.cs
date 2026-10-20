using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.Episodes.Queries.GetEpisodeById;

public record GetEpisodeByIdQuery(int Id) : IRequest<EpisodeDto>;

public class GetEpisodeByIdQueryValidator : AbstractValidator<GetEpisodeByIdQuery>
{
    public GetEpisodeByIdQueryValidator()
    {
    }
}

public class GetEpisodeByIdQueryHandler : IRequestHandler<GetEpisodeByIdQuery, EpisodeDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetEpisodeByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<EpisodeDto> Handle(GetEpisodeByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _context.Episodes
            .Where(x => x.Id == request.Id && x.CreatedBy == _user.Id && x.Work!.Type == WorkType.Story)
            .ProjectTo<EpisodeDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(request.Id, dto);

        return dto;
    }
}
