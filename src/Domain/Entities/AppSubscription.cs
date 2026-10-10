namespace SurveillanceCameras.Domain.Entities;

/// <summary>
/// Gói VIP của người xem (app): còn hạn thì xem được mọi tập. Mỗi người một dòng. <c>CreatedBy</c> là người cấp
/// (quản trị viên), nên chủ gói nằm ở <see cref="UserId"/>.
/// </summary>
public class AppSubscription : BaseAuditableEntity
{
    public string UserId { get; set; } = "";

    /// <summary>Mã gói (vd. <c>week</c>, <c>month</c>, <c>year</c>).</summary>
    public string? Plan { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
