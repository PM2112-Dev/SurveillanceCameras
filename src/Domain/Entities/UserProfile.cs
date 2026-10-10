namespace SurveillanceCameras.Domain.Entities;

/// <summary>
/// Thông tin cơ bản của một kênh trên nền tảng mạng xã hội (TikTok, YouTube…), lưu lại mỗi khi tra cứu kênh.
/// <c>Title</c> là tên hiển thị của kênh.
/// </summary>
public class UserProfile : BaseAuditableEntity
{
    public int UserProfileTypeId { get; set; }

    /// <summary>ID bất biến của kênh trên nền tảng — khoá để nhận ra kênh đã lưu.</summary>
    public string? UserId { get; set; }

    /// <summary>Handle (không kèm @). Có thể bị chủ kênh đổi.</summary>
    public string? UniqueId { get; set; }

    /// <summary>secUid của TikTok — tham số bắt buộc khi gọi các API như drama_list.</summary>
    public string? SecUid { get; set; }

    public string? Signature { get; set; }

    public string? AvatarUrl { get; set; }

    public bool Verified { get; set; }

    public long FollowerCount { get; set; }

    public long FollowingCount { get; set; }

    public long HeartCount { get; set; }

    public long VideoCount { get; set; }

    /// <summary>Lần cuối lấy dữ liệu từ nền tảng.</summary>
    public DateTimeOffset? LastFetchedAt { get; set; }

    public UserProfileType? UserProfileType { get; set; }
}
