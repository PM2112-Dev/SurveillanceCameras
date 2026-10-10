using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models.TikTok;
using SurveillanceCameras.Application.Repository.Media;
using SurveillanceCameras.Application.Repository.TikTok;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.SyncTikTokDramas;

/// <summary>
/// Đồng bộ drama của kênh TikTok đã lưu thành phim (<see cref="Work"/> loại Phim) cùng các tập. Drama đã đồng bộ thì
/// cập nhật thông tin và chỉ thêm tập mới — gọi lại nhiều lần không sinh bản trùng. Chỉ lưu thông tin và link tập,
/// không tải video. Ảnh bìa được tải về kho của hệ thống (link ảnh TikTok hết hạn sau vài ngày).
/// </summary>
public record SyncTikTokDramasCommand : IRequest<SyncTikTokDramasResult>
{
    public const int MaxDramas = 20;

    public int UserProfileId { get; init; }

    public List<string> DramaIds { get; init; } = [];
}

/// <param name="Errors">Lỗi của từng drama không đồng bộ được (drama khác vẫn được lưu).</param>
public record SyncTikTokDramasResult(int Created, int Updated, int EpisodesAdded, List<string> Errors, List<int> MovieIds);

public class SyncTikTokDramasCommandValidator : AbstractValidator<SyncTikTokDramasCommand>
{
    public SyncTikTokDramasCommandValidator()
    {
        RuleFor(x => x.DramaIds)
            .NotEmpty().WithMessage("Chưa chọn drama nào.")
            .Must(ids => ids.Count <= SyncTikTokDramasCommand.MaxDramas)
            .WithMessage($"Mỗi lần đồng bộ tối đa {SyncTikTokDramasCommand.MaxDramas} drama.");
        RuleForEach(x => x.DramaIds).NotEmpty().Matches("^[0-9]{1,30}$").WithMessage("ID drama không hợp lệ.");
    }
}

public class SyncTikTokDramasCommandHandler : IRequestHandler<SyncTikTokDramasCommand, SyncTikTokDramasResult>
{
    private readonly IUser _user;
    private readonly TikTokDramaSyncer _syncer;

    public SyncTikTokDramasCommandHandler(IUser user, TikTokDramaSyncer syncer)
    {
        _user = user;
        _syncer = syncer;
    }

    public Task<SyncTikTokDramasResult> Handle(SyncTikTokDramasCommand request, CancellationToken cancellationToken)
        => _syncer.SyncAsync(_user.Id, request.UserProfileId, request.DramaIds, cancellationToken);
}

/// <summary>
/// Đồng bộ drama TikTok thành phim cho chủ <c>ownerId</c> — dùng chung cho người dùng bấm đồng bộ và tác vụ tự đồng bộ
/// hằng ngày (chạy nền, không có người dùng đăng nhập).
/// </summary>
public class TikTokDramaSyncer
{
    private const int PageSize = 30;

    /// <summary>Chặn vòng lặp nếu TikTok trả hasMore mãi (drama dài nhất vài trăm tập).</summary>
    private const int MaxPages = 50;

    private const int MaxErrorLength = 2000;

    private readonly IApplicationDbContext _context;
    private readonly ITikTokRepository _tikTok;
    private readonly IMediaRepository _media;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TikTokDramaSyncer> _logger;

    public TikTokDramaSyncer(IApplicationDbContext context, ITikTokRepository tikTok,
        IMediaRepository media, TimeProvider timeProvider, ILogger<TikTokDramaSyncer> logger)
    {
        _context = context;
        _tikTok = tikTok;
        _media = media;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <param name="ownerId">Chủ kênh và phim (người dùng hiện tại, hoặc chủ phim khi chạy nền).</param>
    public async Task<SyncTikTokDramasResult> SyncAsync(string? ownerId, int userProfileId, IEnumerable<string> dramaIds,
        CancellationToken cancellationToken)
    {
        var channel = await TikTokChannel.LoadAsync(_context, ownerId, userProfileId, cancellationToken);

        int created = 0, updated = 0, episodesAdded = 0;
        var errors = new List<string>();
        var movieIds = new List<int>();

        foreach (var dramaId in dramaIds.Distinct())
        {
            List<DramaEpisodeDto> episodes;
            try
            {
                episodes = await FetchAllEpisodesAsync(dramaId, cancellationToken);
            }
            catch (TikTokApiException ex)
            {
                _logger.LogWarning("Drama {DramaId}: lấy tập lỗi: {Error}", dramaId, ex.Message);
                errors.Add($"{dramaId}: {ex.Message}");
                await SaveSyncErrorAsync(ownerId, channel.Id, dramaId, ex.Message, cancellationToken);
                continue;
            }

            if (episodes.Count == 0)
            {
                errors.Add($"{dramaId}: TikTok không trả về tập nào (drama không tồn tại hoặc bị ẩn).");
                continue;
            }

            var movie = await _context.Works
                .Include(x => x.Episodes)
                .FirstOrDefaultAsync(x => x.CreatedBy == ownerId && x.Type == WorkType.Movie
                                          && x.UserProfileId == channel.Id && x.ExternalId == dramaId, cancellationToken);
            if (movie is null)
            {
                movie = new Work
                {
                    Type = WorkType.Movie, UserProfileId = channel.Id, ExternalId = dramaId, Episodes = [], CreatedBy = ownerId
                };
                _context.Works.Add(movie);
                created++;
            }
            else
            {
                updated++;
            }

            // Thông tin drama có sẵn trên từng tập.
            var info = episodes.Select(x => x.DramaInfo).FirstOrDefault(x => x?.DramaName is not null);
            movie.Title = info?.DramaName ?? movie.Title ?? dramaId;
            movie.Description = info?.Description ?? movie.Description;
            var remoteCover = CoverOf(info);
            if (!_media.IsStored(movie.CoverUrl)) movie.CoverUrl = remoteCover ?? movie.CoverUrl;
            movie.ViewCount = info is not null ? info.NumWatched : movie.ViewCount;
            movie.IsFull = info is not null && info.NumVideos > 0 && episodes.Count >= info.NumVideos;

            episodesAdded += MergeEpisodes(movie, episodes, channel.UniqueId);
            movie.EpisodesSyncedAt = _timeProvider.GetUtcNow();
            movie.EpisodeSyncError = null;

            await _context.SaveChangesAsync(cancellationToken);
            movieIds.Add(movie.Id);

            if (remoteCover is not null && !_media.IsStored(movie.CoverUrl))
                await StoreCoverAsync(movie, remoteCover, cancellationToken);
        }

        return new SyncTikTokDramasResult(created, updated, episodesAdded, errors, movieIds);
    }

    private async Task<List<DramaEpisodeDto>> FetchAllEpisodesAsync(string dramaId, CancellationToken cancellationToken)
    {
        var all = new List<DramaEpisodeDto>();
        var seen = new HashSet<string>();
        var cursor = "0";

        for (var page = 0; page < MaxPages; page++)
        {
            var result = await _tikTok.FetchDramaEpisodes(null, dramaId, PageSize, cursor, cancellationToken);
            var added = result.ItemList.Where(x => x.Id is not null && seen.Add(x.Id)).ToList();
            all.AddRange(added);

            if (!result.HasMore || added.Count == 0 || string.IsNullOrEmpty(result.Cursor) || result.Cursor == cursor) break;
            cursor = result.Cursor;
        }

        return all;
    }

    /// <summary>Thêm tập mới, cập nhật tập đã có theo ID video.</summary>
    /// <returns>Số tập thêm mới.</returns>
    private static int MergeEpisodes(Work movie, List<DramaEpisodeDto> episodes, string? channelHandle)
    {
        var byId = movie.Episodes!.Where(x => x.ExternalId is not null).ToDictionary(x => x.ExternalId!);
        var added = 0;

        for (var i = 0; i < episodes.Count; i++)
        {
            var item = episodes[i];
            if (!byId.TryGetValue(item.Id!, out var episode))
            {
                episode = new Episode { ExternalId = item.Id, Status = EpisodeStatus.Listed, CreatedBy = movie.CreatedBy };
                movie.Episodes!.Add(episode);
                added++;
            }

            var number = item.EpisodeNumber is > 0 ? item.EpisodeNumber.Value : i + 1;
            var handle = item.Author?.UniqueId ?? channelHandle;

            episode.EpisodeNumber = number;
            episode.Title = $"Tập {number}";
            episode.NameRaw = item.Desc;
            episode.DurationSeconds = item.Video?.Duration is > 0 ? item.Video.Duration : episode.DurationSeconds;
            episode.ThumbnailUrl = item.Video?.Cover ?? episode.ThumbnailUrl;
            episode.VideoUrl = handle is null
                ? $"https://www.tiktok.com/video/{item.Id}"
                : $"https://www.tiktok.com/@{handle}/video/{item.Id}";
        }

        return added;
    }

    /// <summary>Lưu ảnh bìa về kho; lỗi thì giữ link TikTok (còn dùng được vài ngày), lần đồng bộ sau thử lại.</summary>
    private async Task StoreCoverAsync(Work movie, string remoteCover, CancellationToken cancellationToken)
    {
        try
        {
            movie.CoverUrl = await _media.SaveImageFromUrlAsync(remoteCover, "covers/movies", movie.Id.ToString(), cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (MediaException ex)
        {
            _logger.LogWarning("Movie {MovieId}: không lưu được ảnh bìa: {Error}", movie.Id, ex.Message);
        }
    }

    private async Task SaveSyncErrorAsync(string? ownerId, int channelId, string dramaId, string error, CancellationToken cancellationToken)
    {
        var movie = await _context.Works.FirstOrDefaultAsync(x => x.CreatedBy == ownerId && x.Type == WorkType.Movie
            && x.UserProfileId == channelId && x.ExternalId == dramaId, cancellationToken);
        if (movie is null) return;

        movie.EpisodeSyncError = error.Length <= MaxErrorLength ? error : error[..MaxErrorLength];
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? CoverOf(DramaDto? drama)
        => drama is null ? null
            : drama.ZoomCover.TryGetValue("480", out var zoom) ? zoom
            : drama.Cover?.UrlList.FirstOrDefault();
}
