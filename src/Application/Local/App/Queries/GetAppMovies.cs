using Microsoft.Extensions.Options;
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

    /// <summary>
    /// Mã ngôn ngữ người xem chọn (vd. <c>vi</c>). Không truyền, hoặc chưa có phim nào bằng ngôn ngữ đó, thì trả phim
    /// tiếng Anh.
    /// </summary>
    public string? Language { get; init; }

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
    private readonly AppOptions _options;

    public GetAppMoviesQueryHandler(IApplicationDbContext context, IOptions<AppOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<PaginatedList<AppMovieDto>> Handle(GetAppMoviesQuery request, CancellationToken cancellationToken)
    {
        var language = await _context.ResolveAppLanguageAsync(request.Language, cancellationToken);
        var query = _context.PublishedMovies().InLanguage(language);

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
                .OrderByDescending(x => x.BookmarkCount)
                .ThenByDescending(x => x.ViewCount ?? 0)
                .ThenByDescending(x => x.Id),
            _ => query.OrderByDescending(x => x.Created).ThenByDescending(x => x.Id)
        };

        return await PaginatedList<AppMovieDto>.CreateAsync(query.ToAppMovies(_context, _options), request.Page, request.PageSize,
            cancellationToken);
    }
}

/// <summary>Các ngôn ngữ đang có phim đã xuất bản, nhiều phim trước — để app cho người xem chọn.</summary>
public record GetAppLanguagesQuery : IRequest<List<AppLanguageDto>>;

public class GetAppLanguagesQueryHandler : IRequestHandler<GetAppLanguagesQuery, List<AppLanguageDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAppLanguagesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<AppLanguageDto>> Handle(GetAppLanguagesQuery request, CancellationToken cancellationToken)
    {
        var rows = await _context.PublishedMovies()
            .Where(x => x.Language != null && x.Language.Code != null)
            .Select(x => new { x.Language!.Code, x.Language.Title })
            .ToListAsync(cancellationToken);

        // Ngôn ngữ khai báo theo từng người quản trị nên cùng một mã có thể có nhiều bản ghi: gộp theo mã.
        return rows
            .GroupBy(x => x.Code!.ToLowerInvariant())
            .Select(g => new AppLanguageDto(g.Key, g.Select(x => x.Title).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)) ?? g.Key, g.Count()))
            .OrderByDescending(x => x.MovieCount)
            .ThenBy(x => x.Code)
            .ToList();
    }
}

public record GetAppMovieByIdQuery(int Id) : IRequest<AppMovieDto>;

public class GetAppMovieByIdQueryHandler : IRequestHandler<GetAppMovieByIdQuery, AppMovieDto>
{
    private readonly IApplicationDbContext _context;
    private readonly AppOptions _options;

    public GetAppMovieByIdQueryHandler(IApplicationDbContext context, IOptions<AppOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<AppMovieDto> Handle(GetAppMovieByIdQuery request, CancellationToken cancellationToken)
    {
        var movie = await _context.PublishedMovies()
            .Where(x => x.Id == request.Id)
            .ToAppMovies(_context, _options)
            .FirstOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(request.Id, movie);
        return movie;
    }
}
