using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.App.Monetization;

public static class Monetization
{
    /// <summary>Ad unit thử nghiệm của Google: luôn có quảng cáo mẫu, không tính tiền. Thay bằng ad unit thật trên trang quản trị.</summary>
    public static readonly IReadOnlyList<AppAdPlacement> DefaultPlacements =
    [
        new()
        {
            Code = AppAdPlacement.RewardedUnlock, Title = "Xem quảng cáo mở tập", Format = AdFormat.Rewarded,
            AndroidUnitId = "ca-app-pub-3940256099942544/5224354917", IosUnitId = "ca-app-pub-3940256099942544/1712485313",
            DailyLimit = 20
        },
        new()
        {
            Code = AppAdPlacement.InterstitialEpisode, Title = "Quảng cáo khi chuyển tập", Format = AdFormat.Interstitial,
            AndroidUnitId = "ca-app-pub-3940256099942544/1033173712", IosUnitId = "ca-app-pub-3940256099942544/4411468910",
            EveryEpisodes = 3, CooldownSeconds = 180
        },
        new()
        {
            Code = AppAdPlacement.BannerHome, Title = "Banner màn Khám phá", Format = AdFormat.Banner,
            AndroidUnitId = "ca-app-pub-3940256099942544/9214589741", IosUnitId = "ca-app-pub-3940256099942544/2435281174"
        }
    ];

    /// <summary>Cấu hình hiện tại; chưa ai lưu thì dùng giá trị mặc định (không ghi vào DB).</summary>
    public static async Task<AppMonetizationSettings> SettingsAsync(this IApplicationDbContext context, CancellationToken cancellationToken)
        => await context.AppMonetizationSettings.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken)
           ?? new AppMonetizationSettings();

    /// <summary>Các vị trí quảng cáo; chưa khai báo vị trí nào thì dùng bộ mặc định.</summary>
    public static async Task<List<AppAdPlacement>> PlacementsAsync(this IApplicationDbContext context, CancellationToken cancellationToken)
    {
        var placements = await context.AppAdPlacements.AsNoTracking().ToListAsync(cancellationToken);
        return placements.Count > 0 ? placements : DefaultPlacements.ToList();
    }

    public static string FormatName(AdFormat format) => format switch
    {
        AdFormat.Rewarded => "rewarded",
        AdFormat.Interstitial => "interstitial",
        _ => "banner"
    };

    /// <summary>Gói còn hiệu lực có hạn xa nhất của người xem (gồm cả gói quản trị viên cấp).</summary>
    public static Task<AppSubscription?> ActiveSubscriptionAsync(this IApplicationDbContext context, string userId,
        DateTimeOffset now, CancellationToken cancellationToken)
        => context.AppSubscriptions.AsNoTracking()
            .Where(x => x.UserId == userId && x.ExpiresAt > now)
            .OrderByDescending(x => x.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);
}
