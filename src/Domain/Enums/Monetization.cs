namespace SurveillanceCameras.Domain.Enums;

/// <summary>Nơi cấp gói VIP của người xem. Giá trị số được lưu trong DB.</summary>
public enum SubscriptionSource
{
    /// <summary>Quản trị viên cấp tay.</summary>
    Admin = 1,
    AppStore = 2,
    GooglePlay = 3
}

/// <summary>Trạng thái gói mua qua cửa hàng. Có quyền VIP hay không chỉ dựa vào <c>ExpiresAt</c>.</summary>
public enum SubscriptionStatus
{
    /// <summary>Đang hiệu lực và sẽ tự gia hạn.</summary>
    Active = 1,

    /// <summary>Đã tắt tự gia hạn; vẫn dùng tới hết hạn.</summary>
    Canceled = 2,

    /// <summary>Gia hạn bị lỗi thanh toán nhưng cửa hàng vẫn cho dùng trong thời gian ân hạn.</summary>
    GracePeriod = 3,

    /// <summary>Gia hạn lỗi, hết ân hạn: tạm khoá cho tới khi người dùng sửa thanh toán.</summary>
    OnHold = 4,

    Expired = 5,

    /// <summary>Đã hoàn tiền hoặc cửa hàng thu hồi.</summary>
    Revoked = 6,

    /// <summary>Người dùng tạm dừng gói (Google Play).</summary>
    Paused = 7,

    /// <summary>Thanh toán đang chờ xử lý.</summary>
    Pending = 8
}

/// <summary>Loại quảng cáo AdMob. Giá trị số được lưu trong DB.</summary>
public enum AdFormat
{
    /// <summary>Quảng cáo có thưởng: xem hết để mở một tập.</summary>
    Rewarded = 1,

    /// <summary>Quảng cáo toàn màn hình chen giữa các tập.</summary>
    Interstitial = 2,

    Banner = 3
}

/// <summary>Cửa hàng gửi thông báo về gói đăng ký.</summary>
public enum StoreKind
{
    AppStore = 2,
    GooglePlay = 3
}
