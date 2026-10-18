using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Domain.Entities;

/// <summary>
/// Thống kê quảng cáo cộng dồn theo ngày (UTC), vị trí, màn, nền tảng và tiền tệ. App gom sự kiện rồi gửi theo lô nên một
/// dòng ứng với hàng nghìn lượt.
/// </summary>
public class AppAdStat : BaseAuditableEntity
{
    public DateOnly Day { get; set; }

    /// <summary>Mã vị trí (<see cref="AppAdPlacement.Code"/>).</summary>
    public string PlacementCode { get; set; } = "";

    /// <summary>Mã màn đang hiện quảng cáo (<see cref="AppAdScreen.Code"/>); <c>unknown</c> nếu app không báo.</summary>
    public string ScreenCode { get; set; } = "unknown";

    /// <summary><c>android</c> hoặc <c>ios</c>.</summary>
    public string Platform { get; set; } = "android";

    public AdFormat Format { get; set; }

    /// <summary>Tiền tệ của doanh thu (mã ISO 4217); <c>USD</c> nếu app không báo.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Số lần app gọi AdMob xin quảng cáo.</summary>
    public long Requests { get; set; }

    /// <summary>Số lần AdMob trả quảng cáo.</summary>
    public long Filled { get; set; }

    public long Failed { get; set; }

    /// <summary>Số lần quảng cáo thật sự hiện trên màn.</summary>
    public long Impressions { get; set; }

    public long Clicks { get; set; }

    /// <summary>Quảng cáo có thưởng: số lần người xem nhận thưởng.</summary>
    public long Rewarded { get; set; }

    /// <summary>Quảng cáo bị người xem tắt / tự tắt (native hết giờ, toàn màn hình đóng).</summary>
    public long Dismissed { get; set; }

    /// <summary>Doanh thu ước tính do AdMob báo (onPaidEvent), tính bằng một phần triệu đơn vị tiền tệ.</summary>
    public long RevenueMicros { get; set; }

    /// <summary>Lỗi tải quảng cáo gần nhất, để dò khi tỷ lệ lấp đầy thấp.</summary>
    public string? LastError { get; set; }
}
