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

    /// <summary>Mã ngôn ngữ của phim (vd. <c>en</c>); <c>null</c> khi chưa xác định.</summary>
    public string? Language { get; init; }

    /// <summary>
    /// Phim chỉ dành cho VIP (người chưa có VIP không xem được tập nào) — app chỉ gắn nhãn VIP lên phim khi cờ này bật,
    /// không gắn khi phim chỉ có vài tập VIP.
    /// </summary>
    public bool IsVip { get; init; }

    /// <summary>Tập đầu của khoảng mở bằng quảng cáo đang áp dụng cho phim; <c>null</c> là không có tập quảng cáo.</summary>
    public int? AdFromEpisode { get; init; }

    /// <summary>Tập cuối của khoảng quảng cáo; <c>null</c> là tới hết phim.</summary>
    public int? AdToEpisode { get; init; }

    /// <summary>Tập đầu của khoảng chỉ dành cho VIP; <c>null</c> là không có tập VIP.</summary>
    public int? VipFromEpisode { get; init; }

    /// <summary>Tập cuối của khoảng VIP; <c>null</c> là tới hết phim.</summary>
    public int? VipToEpisode { get; init; }
}

/// <summary>Ngôn ngữ đang có phim trên app.</summary>
public record AppLanguageDto(string Code, string Title, int MovieCount);

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

}

/// <summary>
/// Các khoảng tập quảng cáo / VIP đang áp dụng cho một phim. Tập nằm trong cả hai khoảng thì là VIP; tập ngoài cả hai
/// thì miễn phí. Đầu khoảng <c>null</c> là không có khoảng đó; cuối khoảng <c>null</c> là tới hết phim.
/// <paramref name="VipOnly"/> bật thì cả phim chỉ dành cho VIP, bất kể các khoảng.
/// </summary>
public record AppAccessRule(int? AdFrom, int? AdTo, int? VipFrom, int? VipTo, bool VipOnly = false)
{
    /// <summary>Mặc định của hệ thống: N tập đầu miễn phí, M tập kế mở bằng quảng cáo, còn lại VIP.</summary>
    public static AppAccessRule Default(AppOptions options)
    {
        var free = Math.Max(options.FreeEpisodes, 0);
        var ads = Math.Max(options.AdEpisodes, 0);
        return new AppAccessRule(ads > 0 ? free + 1 : null, ads > 0 ? free + ads : null, free + ads + 1, null);
    }

    /// <summary>Khoảng tập đang áp dụng cho <paramref name="movie"/>: cấu hình riêng của phim nếu có, không thì mặc định.</summary>
    public static AppAccessRule Of(Work movie, AppOptions options)
        => (movie.HasCustomAccess
            ? new AppAccessRule(movie.AdFromEpisode, movie.AdToEpisode, movie.VipFromEpisode, movie.VipToEpisode)
            : Default(options)) with { VipOnly = movie.IsVipOnly };

    /// <summary>Quyền xem tập số <paramref name="number"/> với người xem có/không VIP và đã/chưa mở tập bằng quảng cáo.</summary>
    public string Access(int number, bool isVip, bool unlocked)
    {
        if (isVip) return AppAccess.Free;
        if (VipOnly || Contains(VipFrom, VipTo, number)) return AppAccess.Vip;
        if (Contains(AdFrom, AdTo, number)) return unlocked ? AppAccess.Free : AppAccess.Ads;
        return AppAccess.Free;
    }

    private static bool Contains(int? from, int? to, int number)
        => from is { } start && number >= start && (to is not { } end || number <= end);
}

public static class AppQueries
{
    /// <summary>Phim app được thấy: tác phẩm loại Phim đã bật xuất bản.</summary>
    public static IQueryable<Work> PublishedMovies(this IApplicationDbContext context)
        => context.Works.Where(x => x.Type == WorkType.Movie && x.IsPublished);

    public static IQueryable<AppMovieDto> ToAppMovies(this IQueryable<Work> works, IApplicationDbContext context,
        AppOptions options)
    {
        var fallback = AppAccessRule.Default(options);
        int? adFrom = fallback.AdFrom, adTo = fallback.AdTo, vipFrom = fallback.VipFrom, vipTo = fallback.VipTo;

        return works.Select(x => new AppMovieDto
        {
            AdFromEpisode = x.HasCustomAccess ? x.AdFromEpisode : adFrom,
            AdToEpisode = x.HasCustomAccess ? x.AdToEpisode : adTo,
            VipFromEpisode = x.HasCustomAccess ? x.VipFromEpisode : vipFrom,
            VipToEpisode = x.HasCustomAccess ? x.VipToEpisode : vipTo,
            IsVip = x.IsVipOnly,
            Id = x.Id,
            Title = x.Title,
            Description = x.Description,
            CoverUrl = x.CoverUrl,
            Channel = x.UserProfile != null ? x.UserProfile.Title : null,
            EpisodeCount = x.Episodes!.Count,
            ViewCount = x.ViewCount ?? 0,
            SaveCount = context.AppBookmarks.Count(b => b.WorkId == x.Id),
            CommentCount = context.AppComments.Count(c => c.WorkId == x.Id),
            IsFull = x.IsFull,
            Language = x.Language != null ? x.Language.Code : null
        });
    }

    /// <summary>Ngôn ngữ app dùng khi người xem chưa chọn, hoặc chọn ngôn ngữ chưa có phim nào.</summary>
    public const string DefaultLanguage = "en";

    public static IQueryable<Work> InLanguage(this IQueryable<Work> works, string code)
        => works.Where(x => x.Language != null && x.Language.Code != null && x.Language.Code.ToLower() == code);

    /// <summary>
    /// Mã ngôn ngữ dùng để lọc phim cho yêu cầu của app: mã người xem chọn nếu đang có phim đã xuất bản bằng ngôn ngữ
    /// đó, còn lại (không truyền, mã lạ, chưa có phim) là <see cref="DefaultLanguage"/>.
    /// </summary>
    public static async Task<string> ResolveAppLanguageAsync(this IApplicationDbContext context, string? requested,
        CancellationToken cancellationToken)
    {
        var code = Languages.LanguageCodes.Normalize(requested);
        if (code is null || code == DefaultLanguage) return DefaultLanguage;

        return await context.PublishedMovies().InLanguage(code).AnyAsync(cancellationToken) ? code : DefaultLanguage;
    }

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
