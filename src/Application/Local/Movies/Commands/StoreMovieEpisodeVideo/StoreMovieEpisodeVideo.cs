using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.StoreMovieEpisodeVideo;

/// <summary>
/// Tải video của một tập từ TikTok và lưu vào kho ngay (không chờ tác vụ nền) — để thử cookie tài khoản TikTok và tài
/// khoản lưu trữ trên một tập trước khi để hệ thống tự chạy cả kho.
/// </summary>
public record StoreMovieEpisodeVideoCommand(int MovieId, int EpisodeId) : IRequest<StoreMovieEpisodeVideoResult>;

/// <param name="Error">Lý do không lưu được; <c>null</c> khi thành công.</param>
public record StoreMovieEpisodeVideoResult(bool Succeeded, string? StreamUrl, long? SizeBytes, string? Error);

public class StoreMovieEpisodeVideoCommandHandler : IRequestHandler<StoreMovieEpisodeVideoCommand, StoreMovieEpisodeVideoResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly EpisodeVideoStorer _storer;

    public StoreMovieEpisodeVideoCommandHandler(IApplicationDbContext context, IUser user, EpisodeVideoStorer storer)
    {
        _context = context;
        _user = user;
        _storer = storer;
    }

    public async Task<StoreMovieEpisodeVideoResult> Handle(StoreMovieEpisodeVideoCommand request, CancellationToken cancellationToken)
    {
        var episode = await _context.Episodes
            .Include(x => x.Work)
            .FirstOrDefaultAsync(x => x.Id == request.EpisodeId && x.WorkId == request.MovieId
                                      && x.Work!.CreatedBy == _user.Id && x.Work.Type == WorkType.Movie, cancellationToken);

        Guard.Against.NotFound(request.EpisodeId, episode);

        if (episode.StreamUrl is not null)
            return new StoreMovieEpisodeVideoResult(true, episode.StreamUrl, episode.StorageSizeBytes, null);
        if (string.IsNullOrWhiteSpace(episode.VideoUrl))
            return new StoreMovieEpisodeVideoResult(false, null, null, "Tập này không có link video TikTok.");

        try
        {
            await _storer.StoreAsync(episode, cancellationToken);
        }
        catch (TikTokBlockedException)
        {
            // Lỗi đã được ghi vào tập.
        }

        return new StoreMovieEpisodeVideoResult(episode.StreamUrl is not null, episode.StreamUrl, episode.StorageSizeBytes,
            episode.StreamUrl is null ? episode.StorageError : null);
    }
}
