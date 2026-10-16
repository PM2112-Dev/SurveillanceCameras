namespace SurveillanceCameras.Domain.Entities;

/// <summary>
/// Gói VIP bán trong app (<c>Code</c>: <c>week</c>, <c>month</c>, <c>year</c>…; <c>Title</c>: tên hiện cho người xem).
/// Giá do App Store / Google Play quyết định theo product ID; ở đây chỉ quản lý gói nào được bán và hiện ra sao.
/// </summary>
public class AppSubscriptionPlan : BaseAuditableEntity
{
    /// <summary>Product ID của gói tự gia hạn trên App Store Connect.</summary>
    public string? AppleProductId { get; set; }

    /// <summary>Subscription ID (product ID) trên Google Play Console.</summary>
    public string? GoogleProductId { get; set; }

    /// <summary>Thời hạn một kỳ, để quản trị và báo cáo (cửa hàng mới là nơi quyết định hạn thật).</summary>
    public int PeriodDays { get; set; }

    /// <summary>Mô tả ngắn dưới tên gói (vd. "Tiết kiệm 40%").</summary>
    public string? Description { get; set; }

    /// <summary>Số ngày dùng thử đã cấu hình trên cửa hàng, để app ghi chú; 0 là không có.</summary>
    public int TrialDays { get; set; }

    /// <summary>Gói được chọn sẵn và làm nổi bật trên màn mua.</summary>
    public bool IsHighlighted { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
