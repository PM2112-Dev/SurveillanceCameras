namespace SurveillanceCameras.Application.Service.TikTokApi;

/// <summary>Thông tin công khai của một kênh TikTok (lấy từ trang profile).</summary>
public class TikTokUserProfileDto
{
    /// <summary>ID số của người dùng.</summary>
    public string? UserId { get; init; }

    /// <summary>Handle không kèm dấu @ (vd. <c>stardusttv_official</c>).</summary>
    public string? UniqueId { get; init; }

    /// <summary>secUid — tham số bắt buộc của các API như <c>drama_list</c>.</summary>
    public string? SecUid { get; init; }

    public string? Nickname { get; init; }

    public string? Signature { get; init; }

    public string? AvatarUrl { get; init; }

    public bool Verified { get; init; }

    public long FollowerCount { get; init; }

    public long FollowingCount { get; init; }

    public long HeartCount { get; init; }

    public long VideoCount { get; init; }
}
