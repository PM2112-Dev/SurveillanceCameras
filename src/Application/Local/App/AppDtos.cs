using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.App;

/// <summary>Phim hiển thị trên app.</summary>
public class AppMovieDto
{
    public int Id { get; init; }

    public string? Title { get; init; }

    public string? Description { get; init; }

    /// <summary>Đường dẫn ảnh bìa; bắt đầu bằng <c>/media/</c> thì là ảnh trong kho của hệ thống (ghép với địa chỉ máy chủ).</summary>
    public string? CoverUrl { get; init; }

    /// <summary>Tên kênh nguồn của phim.</summary>
    public string? Channel { get; init; }

    public int EpisodeCount { get; init; }

    public long ViewCount { get; init; }

    /// <summary>Số người xem đã lưu phim trên app.</summary>
    public int SaveCount { get; init; }

    public int CommentCount { get; init; }

    public bool IsFull { get; init; }
}

/// <summary>Cách phát một tập.</summary>
/// <param name="Type"><c>stream</c>: <paramref name="Value"/> là link file video; <c>tiktok</c>: là id video TikTok (phát bằng trình phát nhúng).</param>
public record AppPlaybackDto(string Type, string Value);

public class AppEpisodeDto
{
    public int Id { get; init; }

    public int Number { get; init; }

    public int? DurationSeconds { get; init; }

    /// <summary><c>free</c>, <c>ads</c> (mở bằng quảng cáo) hoặc <c>vip</c> — đã tính theo người đang gọi.</summary>
    public string Access { get; init; } = AppAccess.Free;

    /// <summary>Chỉ có khi người gọi xem được tập này (<see cref="Access"/> là <c>free</c>).</summary>
    public AppPlaybackDto? Playback { get; init; }
}

public class AppCommentDto
{
    public int Id { get; init; }

    public string? Author { get; init; }

    public string Content { get; init; } = "";

    public DateTimeOffset Created { get; init; }

    /// <summary>Bình luận của chính người đang gọi.</summary>
    public bool IsMine { get; init; }
}

public class AppHistoryDto
{
    public AppMovieDto Movie { get; init; } = null!;

    public int EpisodeNumber { get; init; }

    public int PositionSeconds { get; init; }

    public int DurationSeconds { get; init; }

    public DateTimeOffset WatchedAt { get; init; }
}

/// <summary>Cấu hình app lấy khi khởi động.</summary>
public record AppConfigDto(int FreeEpisodes, int AdEpisodes, string? PrivacyUrl, string? TermsUrl);

public static class AppAccess
{
    public const string Free = "free";
    public const string Ads = "ads";
    public const string Vip = "vip";

    /// <summary>Quyền xem tập số <paramref name="number"/> với người xem có/không VIP và đã/chưa mở tập bằng quảng cáo.</summary>
    public static string For(AppOptions options, int number, bool isVip, bool unlocked)
    {
        if (isVip || number <= options.FreeEpisodes) return Free;
        if (number <= options.FreeEpisodes + options.AdEpisodes) return unlocked ? Free : Ads;
        return Vip;
    }
}

public static class AppQueries
{
    /// <summary>Phim app được thấy: tác phẩm loại Phim đã bật xuất bản.</summary>
    public static IQueryable<Work> PublishedMovies(this IApplicationDbContext context)
        => context.Works.Where(x => x.Type == WorkType.Movie && x.IsPublished);

    public static IQueryable<AppMovieDto> ToAppMovies(this IQueryable<Work> works, IApplicationDbContext context)
        => works.Select(x => new AppMovieDto
        {
            Id = x.Id,
            Title = x.Title,
            Description = x.Description,
            CoverUrl = x.CoverUrl,
            Channel = x.UserProfile != null ? x.UserProfile.Title : null,
            EpisodeCount = x.Episodes!.Count,
            ViewCount = x.ViewCount ?? 0,
            SaveCount = context.AppBookmarks.Count(b => b.WorkId == x.Id),
            CommentCount = context.AppComments.Count(c => c.WorkId == x.Id),
            IsFull = x.IsFull
        });

    /// <summary>Người xem còn hạn VIP không; <c>null</c> (chưa đăng nhập) thì không.</summary>
    public static Task<bool> IsVipAsync(this IApplicationDbContext context, string? userId, DateTimeOffset now,
        CancellationToken cancellationToken)
        => userId is null
            ? Task.FromResult(false)
            : context.AppSubscriptions.AnyAsync(x => x.UserId == userId && x.ExpiresAt > now, cancellationToken);

    public static string RequireId(this IUser user)
        => user.Id ?? throw new UnauthorizedAccessException();

    public static AppEpisodeDto ToAppEpisode(this Episode episode, string access)
        => new()
        {
            Id = episode.Id,
            Number = episode.EpisodeNumber,
            DurationSeconds = episode.DurationSeconds,
            Access = access,
            Playback = access != AppAccess.Free ? null
                : !string.IsNullOrWhiteSpace(episode.StreamUrl) ? new AppPlaybackDto("stream", episode.StreamUrl)
                : !string.IsNullOrWhiteSpace(episode.ExternalId) ? new AppPlaybackDto("tiktok", episode.ExternalId)
                : null
        };
}
