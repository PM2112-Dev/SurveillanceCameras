using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Movies.Queries.DTOs;

/// <summary>Phim (<see cref="Work"/> loại Phim).</summary>
public class MovieDto : BaseDto
{
    public string? Description { get; init; }

    public string? CoverUrl { get; init; }

    public int? LanguageId { get; init; }

    /// <summary>Mã ngôn ngữ của phim (vd. <c>en</c>).</summary>
    public string? LanguageCode { get; init; }

    public string? LanguageTitle { get; init; }

    /// <summary>ID phim trên nền tảng nguồn (dramaID của TikTok).</summary>
    public string? ExternalId { get; init; }

    public int? UserProfileId { get; init; }

    public string? UserProfileTitle { get; init; }

    /// <summary>Handle của kênh nguồn (để dựng link).</summary>
    public string? UserProfileUniqueId { get; init; }

    /// <summary>Lượt xem trên TikTok tại lần đồng bộ gần nhất.</summary>
    public long? ViewCount { get; init; }

    /// <summary>Đã đồng bộ đủ số tập TikTok báo.</summary>
    public bool IsFull { get; init; }

    /// <summary>Đang hiện trên app xem phim.</summary>
    public bool IsPublished { get; init; }

    public int EpisodeCount { get; init; }

    /// <summary>Tổng thời lượng các tập (giây).</summary>
    public int TotalDurationSeconds { get; init; }

    public DateTimeOffset? EpisodesSyncedAt { get; init; }

    public string? EpisodeSyncError { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Work, MovieDto>()
                .ForMember(d => d.UserProfileTitle, o => o.MapFrom(s => s.UserProfile != null ? s.UserProfile.Title : null))
                .ForMember(d => d.UserProfileUniqueId, o => o.MapFrom(s => s.UserProfile != null ? s.UserProfile.UniqueId : null))
                .ForMember(d => d.LanguageCode, o => o.MapFrom(s => s.Language != null ? s.Language.Code : null))
                .ForMember(d => d.LanguageTitle, o => o.MapFrom(s => s.Language != null ? s.Language.Title : null))
                .ForMember(d => d.EpisodeCount, o => o.MapFrom(s => s.Episodes!.Count))
                .ForMember(d => d.TotalDurationSeconds, o => o.MapFrom(s => s.Episodes!.Sum(e => e.DurationSeconds ?? 0)));
        }
    }
}

/// <summary>Một tập phim.</summary>
public class MovieEpisodeDto : BaseDto
{
    public int EpisodeNumber { get; init; }

    public string? ExternalId { get; init; }

    public string? VideoUrl { get; init; }

    /// <summary>Mã ngôn ngữ của tập (vd. <c>en</c>).</summary>
    public string? LanguageCode { get; init; }

    /// <summary>Link file video tự lưu trữ; có thì app phát bằng link này thay cho trình phát nhúng.</summary>
    public string? StreamUrl { get; init; }

    public string? ThumbnailUrl { get; init; }

    public int? DurationSeconds { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Episode, MovieEpisodeDto>()
                .ForMember(d => d.LanguageCode, o => o.MapFrom(s => s.Language != null ? s.Language.Code : null));
        }
    }
}
