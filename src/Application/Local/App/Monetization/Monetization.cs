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
            AndroidUnitId = "ca-app-pub-3940256099942544/9214589741", IosUnitId = "ca-app-pub-3940256099942544/2435281174",
            ScreenCode = AppAdScreen.Home
        },
        // Native và App Open mặc định tắt: bật khi đã thay ad unit thật.
        new()
        {
            Code = AppAdPlacement.NativeHome, Title = "Native trong danh sách Khám phá", Format = AdFormat.Native, IsEnabled = false,
            AndroidUnitId = "ca-app-pub-3940256099942544/2247696110", IosUnitId = "ca-app-pub-3940256099942544/3986624511",
            ScreenCode = AppAdScreen.Home, EveryItems = 8, DisplaySeconds = 30
        },
        new()
        {
            Code = AppAdPlacement.AppOpen, Title = "Quảng cáo khi mở app", Format = AdFormat.AppOpen, IsEnabled = false,
            AndroidUnitId = "ca-app-pub-3940256099942544/9257395921", IosUnitId = "ca-app-pub-3940256099942544/5575463023",
            CooldownSeconds = 240, DailyLimit = 10
        }
    ];

    /// <summary>Các màn có trong app; mã này app dùng khi báo thống kê và khi chọn quảng cáo cho màn.</summary>
    public static readonly IReadOnlyList<(string Code, string Title)> DefaultScreens =
    [
        (AppAdScreen.Splash, "Khởi động"), (AppAdScreen.Home, "Khám phá"), (AppAdScreen.Search, "Tìm kiếm"),
        (AppAdScreen.Recommended, "Đề xuất"), (AppAdScreen.Watch, "Xem phim"), (AppAdScreen.Library, "Thư viện"),
        (AppAdScreen.Profile, "Cá nhân"), (AppAdScreen.Downloads, "Tải xuống")
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
        AdFormat.Native => "native",
        AdFormat.AppOpen => "app_open",
        _ => "banner"
    };

    public static AdFormat? ParseFormat(string? name) => name?.Trim().ToLowerInvariant() switch
    {
        "rewarded" => AdFormat.Rewarded,
        "interstitial" => AdFormat.Interstitial,
        "banner" => AdFormat.Banner,
        "native" => AdFormat.Native,
        "app_open" => AdFormat.AppOpen,
        _ => null
    };

    /// <summary>Chỉ các loại này hiện trong một màn cụ thể nên phải chọn màn.</summary>
    public static bool NeedsScreen(AdFormat format) => format is AdFormat.Banner or AdFormat.Native;

    /// <summary>Thêm các màn mặc định còn thiếu. Trả về <c>true</c> nếu có thêm.</summary>
    public static async Task<bool> EnsureScreensAsync(this IApplicationDbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.AppAdScreens.Select(x => x.Code).ToListAsync(cancellationToken);
        var missing = DefaultScreens.Where(x => !existing.Contains(x.Code)).ToList();
        if (missing.Count == 0) return false;
        var order = existing.Count;
        context.AppAdScreens.AddRange(missing.Select(x => new AppAdScreen { Code = x.Code, Title = x.Title, SortOrder = order++ }));
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Gói còn hiệu lực có hạn xa nhất của người xem (gồm cả gói quản trị viên cấp).</summary>
    public static Task<AppSubscription?> ActiveSubscriptionAsync(this IApplicationDbContext context, string userId,
        DateTimeOffset now, CancellationToken cancellationToken)
        => context.AppSubscriptions.AsNoTracking()
            .Where(x => x.UserId == userId && x.ExpiresAt > now)
            .OrderByDescending(x => x.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);
}
