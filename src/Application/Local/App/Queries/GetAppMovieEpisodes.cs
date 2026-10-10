using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.App.Queries;

/// <summary>
/// Mọi tập của một phim đã xuất bản theo thứ tự tập, kèm quyền xem của người đang gọi (chưa đăng nhập thì chỉ các tập
/// miễn phí). Cách phát chỉ trả về cho tập xem được.
/// </summary>
public record GetAppMovieEpisodesQuery(int MovieId) : IRequest<List<AppEpisodeDto>>;

public class GetAppMovieEpisodesQueryHandler : IRequestHandler<GetAppMovieEpisodesQuery, List<AppEpisodeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;
    private readonly AppOptions _options;

    public GetAppMovieEpisodesQueryHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider,
        IOptions<AppOptions> options)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<List<AppEpisodeDto>> Handle(GetAppMovieEpisodesQuery request, CancellationToken cancellationToken)
    {
        if (!await _context.PublishedMovies().AnyAsync(x => x.Id == request.MovieId, cancellationToken))
            throw new NotFoundException(request.MovieId.ToString(), "Movie");

        var episodes = await _context.Episodes
            .AsNoTracking()
            .Where(x => x.WorkId == request.MovieId)
            .OrderBy(x => x.EpisodeNumber)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var userId = _user.Id;
        var isVip = await _context.IsVipAsync(userId, _timeProvider.GetUtcNow(), cancellationToken);
        var unlocked = userId is null || isVip
            ? []
            : (await _context.AppEpisodeUnlocks
                .Where(x => x.CreatedBy == userId && x.Episode!.WorkId == request.MovieId)
                .Select(x => x.EpisodeId)
                .ToListAsync(cancellationToken)).ToHashSet();

        return episodes
            .Select(x => x.ToAppEpisode(AppAccess.For(_options, x.EpisodeNumber, isVip, unlocked.Contains(x.Id))))
            .ToList();
    }
}
