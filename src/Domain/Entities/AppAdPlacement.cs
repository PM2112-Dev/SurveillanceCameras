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

    public AdFormat Format { get; set; }

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
