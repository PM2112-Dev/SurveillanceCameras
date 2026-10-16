namespace SurveillanceCameras.Domain.Entities;

/// <summary>
/// Một lượt xem hết quảng cáo có thưởng, đã được AdMob xác nhận qua server-side verification (SSV). Chủ là
/// <see cref="UserId"/>; dùng để mở tập <see cref="EpisodeId"/> một lần.
/// </summary>
public class AppAdReward : BaseAuditableEntity
{
    public string UserId { get; set; } = "";

    public int? EpisodeId { get; set; }

    /// <summary><c>transaction_id</c> của AdMob — mỗi lượt xem một mã, chống ghi trùng.</summary>
    public string TransactionId { get; set; } = "";

    public string? AdUnit { get; set; }

    public DateTimeOffset RewardedAt { get; set; }

    /// <summary>Đã dùng để mở tập.</summary>
    public DateTimeOffset? ConsumedAt { get; set; }
}
