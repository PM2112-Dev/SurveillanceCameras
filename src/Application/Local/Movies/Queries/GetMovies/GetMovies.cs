using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Movies.Queries.DTOs;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Queries.GetMovies;

public enum MovieSort
{
    /// <summary>Mới đồng bộ trước.</summary>
    Newest = 0,

    /// <summary>Lượt xem từ cao đến thấp (phim chưa có lượt xem xếp cuối).</summary>
    MostViewed = 1
}

public record GetMoviesQuery : IRequest<PaginatedList<MovieDto>>
{
    public string? Title { get; init; }

    /// <summary>Chỉ phim của kênh này.</summary>
    public int? UserProfileId { get; init; }

    /// <summary>Chỉ phim thuộc ngôn ngữ này.</summary>
    public int? LanguageId { get; init; }

    public MovieSort Sort { get; init; } = MovieSort.Newest;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public class GetMoviesQueryValidator : AbstractValidator<GetMoviesQuery>
{
    public GetMoviesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.Sort).IsInEnum();
    }
}

public class GetMoviesQueryHandler : IRequestHandler<GetMoviesQuery, PaginatedList<MovieDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public GetMoviesQueryHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public Task<PaginatedList<MovieDto>> Handle(GetMoviesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Works.Where(x => x.CreatedBy == _user.Id && x.Type == WorkType.Movie);

        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => x.Title != null && x.Title.Contains(request.Title));
        if (request.UserProfileId is { } userProfileId)
            query = query.Where(x => x.UserProfileId == userProfileId);
        if (request.LanguageId is { } languageId)
            query = query.Where(x => x.LanguageId == languageId);

        return PaginatedList<MovieDto>.CreateAsync(
            Sorted(query, request.Sort).ProjectTo<MovieDto>(_mapper.ConfigurationProvider),
            request.Page, request.PageSize, cancellationToken);
    }

    private static IQueryable<Domain.Entities.Work> Sorted(IQueryable<Domain.Entities.Work> query, MovieSort sort)
        => sort == MovieSort.MostViewed
            ? query.OrderBy(x => x.ViewCount == null).ThenByDescending(x => x.ViewCount).ThenByDescending(x => x.Id)
            : query.OrderByDescending(x => x.Created).ThenByDescending(x => x.Id);
}
