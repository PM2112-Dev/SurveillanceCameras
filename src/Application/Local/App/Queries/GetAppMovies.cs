using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;

namespace SurveillanceCameras.Application.Local.App.Queries;

public enum AppMovieSort
{
    /// <summary>Mới thêm trước.</summary>
    Newest = 0,

    /// <summary>Lượt xem từ cao đến thấp.</summary>
    MostViewed = 1,

    /// <summary>Được lưu nhiều nhất trên app, rồi tới lượt xem.</summary>
    Top = 2
}

/// <summary>Danh sách phim đã xuất bản cho app (không cần đăng nhập).</summary>
public record GetAppMoviesQuery : IRequest<PaginatedList<AppMovieDto>>
{
    /// <summary>Tìm trong tên phim và tên kênh, không phân biệt hoa thường.</summary>
    public string? Search { get; init; }

    public AppMovieSort Sort { get; init; } = AppMovieSort.Newest;

    /// <summary>Chỉ phim đã đủ tập.</summary>
    public bool? Full { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public class GetAppMoviesQueryValidator : AbstractValidator<GetAppMoviesQuery>
{
    public GetAppMoviesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Sort).IsInEnum();
        RuleFor(x => x.Search).MaximumLength(100);
    }
}

public class GetAppMoviesQueryHandler : IRequestHandler<GetAppMoviesQuery, PaginatedList<AppMovieDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAppMoviesQueryHandler(IApplicationDbContext context) => _context = context;

    public Task<PaginatedList<AppMovieDto>> Handle(GetAppMoviesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PublishedMovies();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(x => (x.Title != null && x.Title.ToLower().Contains(term))
                                     || (x.UserProfile != null && x.UserProfile.Title != null
                                         && x.UserProfile.Title.ToLower().Contains(term)));
        }

        if (request.Full == true) query = query.Where(x => x.IsFull);

        query = request.Sort switch
        {
            AppMovieSort.MostViewed => query.OrderByDescending(x => x.ViewCount ?? 0).ThenByDescending(x => x.Id),
            AppMovieSort.Top => query
                .OrderByDescending(x => _context.AppBookmarks.Count(b => b.WorkId == x.Id))
                .ThenByDescending(x => x.ViewCount ?? 0)
                .ThenByDescending(x => x.Id),
            _ => query.OrderByDescending(x => x.Created).ThenByDescending(x => x.Id)
        };

        return PaginatedList<AppMovieDto>.CreateAsync(query.ToAppMovies(_context), request.Page, request.PageSize,
            cancellationToken);
    }
}

public record GetAppMovieByIdQuery(int Id) : IRequest<AppMovieDto>;

public class GetAppMovieByIdQueryHandler : IRequestHandler<GetAppMovieByIdQuery, AppMovieDto>
{
    private readonly IApplicationDbContext _context;

    public GetAppMovieByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<AppMovieDto> Handle(GetAppMovieByIdQuery request, CancellationToken cancellationToken)
    {
        var movie = await _context.PublishedMovies()
            .Where(x => x.Id == request.Id)
            .ToAppMovies(_context)
            .FirstOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(request.Id, movie);
        return movie;
    }
}
