using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Domain.Entities;

/// <summary>
/// Một gói VIP của người xem (app): còn hạn (<see cref="ExpiresAt"/> chưa qua) thì xem được mọi tập. Mỗi người có tối đa
/// một gói do quản trị viên cấp và một dòng cho mỗi gói mua qua cửa hàng. <c>CreatedBy</c> là người/hệ thống tạo dòng,
/// chủ gói nằm ở <see cref="UserId"/>.
/// </summary>
public class AppSubscription : BaseAuditableEntity
{
    public string UserId { get; set; } = "";

    /// <summary>Mã gói (<see cref="AppSubscriptionPlan"/>.Code, vd. <c>month</c>).</summary>
    public string? Plan { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public SubscriptionSource Source { get; set; } = SubscriptionSource.Admin;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    /// <summary>Product ID trên cửa hàng.</summary>
    public string? ProductId { get; set; }

    /// <summary>
    /// Khoá của gói ở cửa hàng: <c>originalTransactionId</c> (App Store) hoặc purchase token mới nhất (Google Play).
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>Purchase token cũ mà token hiện tại thay thế (Google Play, khi đổi gói hoặc mua lại).</summary>
    public string? PreviousExternalId { get; set; }

    public bool AutoRenew { get; set; }

    /// <summary>Giao dịch thử (sandbox / tài khoản test).</summary>
    public bool IsSandbox { get; set; }

    /// <summary>Lần gần nhất đối chiếu với cửa hàng.</summary>
    public DateTimeOffset? VerifiedAt { get; set; }
}
