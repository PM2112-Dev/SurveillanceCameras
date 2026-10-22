using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.Commands.DeleteMovieEpisodes;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.CleanupMovieEpisodes;

/// <summary>
/// Dọn các tập lỗi / trùng của phim. Chạy thử (<see cref="Apply"/> = false) chỉ báo sẽ làm gì.
/// <list type="bullet">
/// <item>Tập trùng số tập và cùng nội dung (thời lượng gần nhau): giữ một tập (ưu tiên tập đã có file phát), xoá bản sao.</item>
/// <item>Tập không còn nguồn nào để phát (không link TikTok, không file tự lưu): xoá khi <see cref="RemoveBroken"/>.</item>
/// <item>Tập lưu video lỗi: cho thử lại (xoá lỗi, đặt lại số lần thử).</item>
/// <item>Tập trùng số nhưng nội dung khác nhau: báo là xung đột; <see cref="Renumber"/> đánh số lại toàn bộ phim 1…N theo thứ tự hiện tại.</item>
/// </list>
/// </summary>
public record CleanupMovieEpisodesCommand : IRequest<CleanupMovieEpisodesResult>
{
    public int MovieId { get; init; }

    public bool Apply { get; init; }

    public bool RemoveBroken { get; init; } = true;

    public bool Renumber { get; init; }
}

/// <param name="Duplicates">Số tập là bản sao (bị xoá khi áp dụng).</param>
/// <param name="Broken">Số tập không còn nguồn phát.</param>
/// <param name="Conflicts">Số nhóm cùng số tập nhưng khác nội dung.</param>
/// <param name="StorageRetried">Số tập lưu video lỗi được cho thử lại.</param>
/// <param name="Renumbered">Số tập được đổi số.</param>
public record CleanupMovieEpisodesResult(bool Applied, int Duplicates, int Broken, int Conflicts, int StorageRetried, int Renumbered,
    List<string> Details);

public class CleanupMovieEpisodesCommandHandler : IRequestHandler<CleanupMovieEpisodesCommand, CleanupMovieEpisodesResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly EpisodeVideoStorer _videoStorer;

    public CleanupMovieEpisodesCommandHandler(IApplicationDbContext context, IUser user, EpisodeVideoStorer videoStorer)
    {
        _context = context;
        _user = user;
        _videoStorer = videoStorer;
    }

    public async Task<CleanupMovieEpisodesResult> Handle(CleanupMovieEpisodesCommand request, CancellationToken cancellationToken)
    {
        var movie = await _context.Works.FirstOrDefaultAsync(
            x => x.Id == request.MovieId && x.CreatedBy == _user.Id && x.Type == WorkType.Movie, cancellationToken);
        Guard.Against.NotFound(request.MovieId, movie);

        var infos = (await _context.Episodes.AsNoTracking().Where(x => x.WorkId == movie.Id).ToListAsync(cancellationToken))
            .Select(MovieEpisodeIssues.ToInfo).ToList();

        var details = new List<string>();
        var remove = new HashSet<int>();
        var conflicts = 0;

        foreach (var group in MovieEpisodeIssues.DuplicateNumberGroups(infos))
        {
            var clusters = MovieEpisodeIssues.ClusterBySameContent(group);
            foreach (var cluster in clusters.Where(c => c.Count > 1))
            {
                var keeper = MovieEpisodeIssues.Keeper(cluster);
                foreach (var copy in cluster.Where(x => x.Id != keeper.Id)) remove.Add(copy.Id);
                details.Add($"Tập {group[0].EpisodeNumber}: {cluster.Count} bản sao, giữ #{keeper.Id}, xoá {cluster.Count - 1}.");
            }

            if (clusters.Count > 1)
            {
                conflicts++;
                details.Add($"Tập {group[0].EpisodeNumber}: {clusters.Count} nội dung khác nhau cùng số — cần đánh số lại.");
            }
        }

        var broken = request.RemoveBroken
            ? infos.Where(x => !remove.Contains(x.Id) && MovieEpisodeIssues.IsMissingSource(x)).ToList()
            : [];
        foreach (var item in broken)
        {
            remove.Add(item.Id);
            details.Add($"Tập {item.EpisodeNumber} (#{item.Id}): không còn link TikTok hay file phát — xoá.");
        }

        var retry = infos.Where(x => !remove.Contains(x.Id) && MovieEpisodeIssues.IsStorageFailed(x)).Select(x => x.Id).ToList();
        if (retry.Count > 0) details.Add($"{retry.Count} tập lưu video lỗi — cho thử lại.");

        // Số tập sau khi xoá bản sao: đánh số lại 1…N theo thứ tự (số tập, Id) hiện tại.
        var renumber = request.Renumber
            ? infos.Where(x => !remove.Contains(x.Id)).OrderBy(x => x.EpisodeNumber).ThenBy(x => x.Id)
                .Select((x, index) => (x.Id, Old: x.EpisodeNumber, New: index + 1)).Where(x => x.Old != x.New).ToList()
            : [];
        if (renumber.Count > 0) details.Add($"Đánh số lại {renumber.Count} tập (1…{infos.Count - remove.Count}).");

        var duplicateCount = remove.Count - broken.Count;
        if (request.Apply)
        {
            await _videoStorer.ReleaseAndRetryAsync(_context, retry, cancellationToken);

            if (renumber.Count > 0)
            {
                var map = renumber.ToDictionary(x => x.Id, x => x.New);
                var tracked = await _context.Episodes.Where(x => x.WorkId == movie.Id && map.Keys.Contains(x.Id)).ToListAsync(cancellationToken);
                foreach (var episode in tracked)
                {
                    episode.EpisodeNumber = map[episode.Id];
                    episode.Title = $"Tập {episode.EpisodeNumber}";
                    episode.IsEdited = true;
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            if (remove.Count > 0)
                await DeleteMovieEpisodesCommandHandler.RemoveAsync(_context, _videoStorer, movie, remove, cancellationToken);
        }

        return new CleanupMovieEpisodesResult(request.Apply, duplicateCount, broken.Count, conflicts, retry.Count, renumber.Count, details);
    }
}
