using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.App.Queries;

/// <summary>Phim người đang đăng nhập đã lưu, mới lưu trước.</summary>
public record GetMyBookmarksQuery : IRequest<List<AppMovieDto>>;

public class GetMyBookmarksQueryHandler : IRequestHandler<GetMyBookmarksQuery, List<AppMovieDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetMyBookmarksQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<List<AppMovieDto>> Handle(GetMyBookmarksQuery request, CancellationToken cancellationToken)
    {
        var userId = _user.RequireId();
        var ids = await _context.AppBookmarks
            .Where(x => x.CreatedBy == userId)
            .OrderByDescending(x => x.Created)
            .ThenByDescending(x => x.Id)
            .Select(x => x.WorkId)
            .ToListAsync(cancellationToken);

        var movies = await _context.PublishedMovies()
            .Where(x => ids.Contains(x.Id))
            .ToAppMovies(_context)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        // Phim đã gỡ xuất bản không hiện, nhưng lượt lưu vẫn giữ để hiện lại khi phim xuất bản trở lại.
        return ids.Where(movies.ContainsKey).Select(id => movies[id]).ToList();
    }
}

/// <summary>Lịch sử xem của người đang đăng nhập, mới xem trước.</summary>
public record GetMyHistoryQuery : IRequest<List<AppHistoryDto>>;

public class GetMyHistoryQueryHandler : IRequestHandler<GetMyHistoryQuery, List<AppHistoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetMyHistoryQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<List<AppHistoryDto>> Handle(GetMyHistoryQuery request, CancellationToken cancellationToken)
    {
        var userId = _user.RequireId();
        var entries = await _context.AppWatchHistories
            .AsNoTracking()
            .Where(x => x.CreatedBy == userId)
            .OrderByDescending(x => x.WatchedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        var ids = entries.Select(x => x.WorkId).ToList();
        var movies = await _context.PublishedMovies()
            .Where(x => ids.Contains(x.Id))
            .ToAppMovies(_context)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return entries
            .Where(x => movies.ContainsKey(x.WorkId))
            .Select(x => new AppHistoryDto
            {
                Movie = movies[x.WorkId],
                EpisodeNumber = x.EpisodeNumber,
                PositionSeconds = x.PositionSeconds,
                DurationSeconds = x.DurationSeconds,
                WatchedAt = x.WatchedAt
            })
            .ToList();
    }
}
