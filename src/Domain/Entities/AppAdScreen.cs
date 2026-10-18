namespace SurveillanceCameras.Domain.Entities;

/// <summary>
/// Một màn hình trong app được khai báo trên máy chủ. <c>Code</c> là tên app dùng khi báo thống kê (vd. <c>home</c>);
/// vị trí quảng cáo gắn với màn qua <see cref="AppAdPlacement.ScreenCode"/>. Tắt màn thì mọi quảng cáo của màn đó ngừng hiện.
/// </summary>
public class AppAdScreen : BaseAuditableEntity
{
    public const string Splash = "splash";
    public const string Home = "home";
    public const string Search = "search";
    public const string Recommended = "recommended";
    public const string Watch = "watch";
    public const string Library = "library";
    public const string Profile = "profile";
    public const string Downloads = "downloads";

    public bool IsEnabled { get; set; } = true;

    public int SortOrder { get; set; }
}
