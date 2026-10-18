namespace SurveillanceCameras.Domain.Entities;

/// <summary>Cấu hình kiếm tiền của app (một dòng duy nhất), quản trị viên đổi được mà không cần phát hành lại app.</summary>
public class AppMonetizationSettings : BaseAuditableEntity
{
    /// <summary>Công tắc tổng: tắt thì app không tải SDK và không hiện quảng cáo nào.</summary>
    public bool AdsEnabled { get; set; } = true;

    /// <summary>Tắt thì app ẩn nút mua gói (vd. khi cửa hàng đang lỗi).</summary>
    public bool PurchasesEnabled { get; set; } = true;

    /// <summary>
    /// Bắt buộc AdMob xác nhận lượt xem (SSV) trước khi mở tập bằng quảng cáo. Chỉ bật sau khi đã khai báo URL callback
    /// SSV cho ad unit thật trên AdMob, nếu không người xem sẽ không mở được tập.
    /// </summary>
    public bool RequireAdVerification { get; set; }

    /// <summary>Mẫu màn mua gói app hiện (<c>A</c>–<c>D</c>).</summary>
    public string PaywallVariant { get; set; } = "A";

    /// <summary>Mời mua gói sau mỗi bao nhiêu lần xem quảng cáo mở tập; 0 là không mời.</summary>
    public int PaywallAfterRewardedAds { get; set; } = 3;

    /// <summary>Phiên bản app tối thiểu (vd. <c>1.2.0</c>); bản cũ hơn được nhắc cập nhật.</summary>
    public string? MinAppVersion { get; set; }

    /// <summary>Tắt thì app ngừng gửi thống kê quảng cáo (số request, hiển thị, bấm, doanh thu).</summary>
    public bool AdStatsEnabled { get; set; } = true;
}
