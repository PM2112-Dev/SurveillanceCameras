using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Domain.Entities;

/// <summary>
/// Một vị trí hiện quảng cáo AdMob trong app. <c>Code</c> là khoá app dùng (<see cref="RewardedUnlock"/>…), <c>Title</c>
/// là tên cho quản trị viên.
/// </summary>
public class AppAdPlacement : BaseAuditableEntity
{
    /// <summary>Xem quảng cáo có thưởng để mở tập.</summary>
    public const string RewardedUnlock = "rewarded_unlock";

    /// <summary>Quảng cáo toàn màn hình khi chuyển tập.</summary>
    public const string InterstitialEpisode = "interstitial_episode";

    /// <summary>Banner cuối màn Khám phá.</summary>
    public const string BannerHome = "banner_home";

    /// <summary>Quảng cáo native chèn trong danh sách (mã màn xem <see cref="AppAdScreen"/>).</summary>
    public const string NativeHome = "native_home";

    /// <summary>Quảng cáo khi mở app hoặc quay lại app.</summary>
    public const string AppOpen = "app_open";

    public AdFormat Format { get; set; }

    /// <summary>
    /// Mã màn (<see cref="AppAdScreen.Code"/>) hiện quảng cáo; để trống là mọi màn. Banner và native chỉ hiện ở màn này.
    /// Nhiều vị trí cùng loại, cùng màn được phép: app thử lần lượt theo <see cref="Priority"/> cho tới khi có quảng cáo.
    /// </summary>
    public string? ScreenCode { get; set; }

    /// <summary>Thứ tự thử khi nhiều vị trí cùng loại, cùng màn: số nhỏ trước.</summary>
    public int Priority { get; set; }

    /// <summary>Native: tự tắt sau bấy nhiêu giây kể từ lúc hiện; 0 là không tự tắt.</summary>
    public int DisplaySeconds { get; set; }

    /// <summary>Native: chèn một quảng cáo sau mỗi bao nhiêu mục của danh sách; 0 là chỉ chèn một lần ở đầu.</summary>
    public int EveryItems { get; set; }

    public string? AndroidUnitId { get; set; }

    public string? IosUnitId { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>Số lần tối đa mỗi người mỗi ngày (giờ UTC); 0 là không giới hạn.</summary>
    public int DailyLimit { get; set; }

    /// <summary>Khoảng nghỉ tối thiểu giữa hai lần hiện, tính bằng giây.</summary>
    public int CooldownSeconds { get; set; }

    /// <summary>Quảng cáo chen giữa: hiện sau mỗi bao nhiêu tập được phát; 0 là tắt.</summary>
    public int EveryEpisodes { get; set; }
}
