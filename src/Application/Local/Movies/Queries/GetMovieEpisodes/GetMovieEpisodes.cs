using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Movies.Queries.DTOs;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Queries.GetMovieEpisodes;

/// <summary>Các tập của phim theo thứ tự tập.</summary>
public record GetMovieEpisodesQuery : IRequest<PaginatedList<MovieEpisodeDto>>
{
    public int MovieId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}

public class GetMovieEpisodesQueryValidator : AbstractValidator<GetMovieEpisodesQuery>
{
    public GetMovieEpisodesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public class GetMovieEpisodesQueryHandler : IRequestHandler<GetMovieEpisodesQuery, PaginatedList<MovieEpisodeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public GetMovieEpisodesQueryHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public async Task<PaginatedList<MovieEpisodeDto>> Handle(GetMovieEpisodesQuery request, CancellationToken cancellationToken)
    {
        var exists = await _context.Works.AnyAsync(
            x => x.Id == request.MovieId && x.CreatedBy == _user.Id && x.Type == WorkType.Movie, cancellationToken);
        if (!exists) throw new NotFoundException(request.MovieId.ToString(), "Movie");

        return await PaginatedList<MovieEpisodeDto>.CreateAsync(
            _context.Episodes
                .Where(x => x.WorkId == request.MovieId)
                .OrderBy(x => x.EpisodeNumber)
                .ProjectTo<MovieEpisodeDto>(_mapper.ConfigurationProvider),
            request.Page, request.PageSize, cancellationToken);
    }
}
