using SurveillanceCameras.Application.Local.UserProfileTypes.Queries.DTOs;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.UserProfiles.Queries.DTOs;

public class UserProfileDto : BaseDto
{
    public int UserProfileTypeId { get; init; }

    public string? UserId { get; init; }

    public string? UniqueId { get; init; }

    public string? SecUid { get; init; }

    public string? Signature { get; init; }

    public string? AvatarUrl { get; init; }

    public bool Verified { get; init; }

    public long FollowerCount { get; init; }

    public long FollowingCount { get; init; }

    public long HeartCount { get; init; }

    public long VideoCount { get; init; }

    public DateTimeOffset? LastFetchedAt { get; init; }

    /// <summary>Tự kiểm tra phim mới và tập mới theo định kỳ.</summary>
    public bool AutoSyncMovies { get; init; }

    public DateTimeOffset? MoviesSyncedAt { get; init; }

    public string? MoviesSyncError { get; init; }

    public UserProfileTypeDto? UserProfileType { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<UserProfile, UserProfileDto>();
        }
    }
}
