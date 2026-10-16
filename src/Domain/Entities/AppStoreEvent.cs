using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Domain.Entities;

/// <summary>Thông báo App Store / Google Play gửi về (gia hạn, huỷ, hoàn tiền…), giữ nguyên để tra soát và chống xử lý trùng.</summary>
public class AppStoreEvent : BaseAuditableEntity
{
    public StoreKind Store { get; set; }

    /// <summary><c>notificationUUID</c> (App Store) hoặc <c>messageId</c> của Pub/Sub (Google Play).</summary>
    public string NotificationId { get; set; } = "";

    /// <summary>Loại thông báo (vd. <c>DID_RENEW</c>, <c>SUBSCRIPTION_CANCELED</c>).</summary>
    public string? Type { get; set; }

    public string? ExternalId { get; set; }

    public string Payload { get; set; } = "";

    public DateTimeOffset? ProcessedAt { get; set; }

    public string? Error { get; set; }
}
