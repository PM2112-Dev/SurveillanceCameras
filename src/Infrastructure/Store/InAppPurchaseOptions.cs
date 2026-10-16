namespace SurveillanceCameras.Infrastructure.Store;

/// <summary>Cấu hình xác minh mua trong app và quảng cáo (mục <c>Store</c> trong appsettings).</summary>
public class InAppPurchaseOptions
{
    public const string SectionName = "Store";

    public AppStoreOptions AppStore { get; set; } = new();

    public GooglePlayOptions GooglePlay { get; set; } = new();
}

public class AppStoreOptions
{
    /// <summary>Bundle ID của app iOS; giao dịch của app khác bị từ chối. Bỏ trống thì tắt mua trên iOS.</summary>
    public string? BundleId { get; set; }

    /// <summary>Nhận giao dịch sandbox/TestFlight. Tắt trên máy chủ thật khi không cần thử nữa.</summary>
    public bool AllowSandbox { get; set; } = true;
}

public class GooglePlayOptions
{
    /// <summary>Application ID của app Android (vd. <c>com.mte.movie.movieai</c>).</summary>
    public string? PackageName { get; set; }

    /// <summary>
    /// Nội dung file JSON khoá của service account có quyền xem đơn hàng trên Google Play Console. Có thể dùng
    /// <see cref="ServiceAccountFile"/> thay thế.
    /// </summary>
    public string? ServiceAccountJson { get; set; }

    /// <summary>Đường dẫn file JSON khoá service account.</summary>
    public string? ServiceAccountFile { get; set; }

    /// <summary>Mã bí mật gắn vào URL push của Pub/Sub (<c>?token=…</c>) để chỉ Google gọi được.</summary>
    public string? NotificationToken { get; set; }
}
