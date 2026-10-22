using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.DeleteMovieEpisodes;

/// <summary>
/// Xoá một hay nhiều tập phim (chỉ trong hệ thống, không đụng TikTok), kèm file video đã lưu ở kho. ID video của tập được ghi
/// nhớ ở phim để đồng bộ lại không thêm tập đó trở lại.
/// </summary>
/// <returns>Số tập đã xoá.</returns>
public record DeleteMovieEpisodesCommand(int MovieId, List<int> EpisodeIds) : IRequest<int>;

public class DeleteMovieEpisodesCommandValidator : AbstractValidator<DeleteMovieEpisodesCommand>
{
    public DeleteMovieEpisodesCommandValidator()
    {
        RuleFor(x => x.EpisodeIds).NotEmpty().WithMessage("Chưa chọn tập nào.")
            .Must(ids => ids.Count <= 500).WithMessage("Mỗi lần xoá tối đa 500 tập.");
    }
}

public class DeleteMovieEpisodesCommandHandler : IRequestHandler<DeleteMovieEpisodesCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly EpisodeVideoStorer _videoStorer;

    public DeleteMovieEpisodesCommandHandler(IApplicationDbContext context, IUser user, EpisodeVideoStorer videoStorer)
    {
        _context = context;
        _user = user;
        _videoStorer = videoStorer;
    }

    public async Task<int> Handle(DeleteMovieEpisodesCommand request, CancellationToken cancellationToken)
    {
        var movie = await _context.Works.FirstOrDefaultAsync(
            x => x.Id == request.MovieId && x.CreatedBy == _user.Id && x.Type == WorkType.Movie, cancellationToken);
        Guard.Against.NotFound(request.MovieId, movie);

        return await RemoveAsync(_context, _videoStorer, movie, request.EpisodeIds, cancellationToken);
    }

    /// <summary>Xoá các tập của phim (đã nạp) — dùng chung với tác vụ dọn tập trùng.</summary>
    internal static async Task<int> RemoveAsync(IApplicationDbContext context, EpisodeVideoStorer videoStorer,
        Domain.Entities.Work movie, IReadOnlyCollection<int> episodeIds, CancellationToken cancellationToken)
    {
        var episodes = await context.Episodes
            .Where(x => x.WorkId == movie.Id && episodeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        if (episodes.Count == 0) return 0;

        await videoStorer.ReleaseAsync(episodes.Where(x => x.StorageAccountId != null).ToList(), cancellationToken);

        var excluded = movie.ExcludedEpisodeIds.ToHashSet();
        foreach (var id in episodes.Select(x => x.ExternalId).OfType<string>()) excluded.Add(id);
        // Gán lại danh sách mới để EF nhận ra thay đổi.
        movie.ExcludedEpisodeIds = [.. excluded];

        context.Episodes.RemoveRange(episodes);
        await context.SaveChangesAsync(cancellationToken);
        return episodes.Count;
    }
}
