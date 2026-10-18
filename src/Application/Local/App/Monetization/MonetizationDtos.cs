namespace SurveillanceCameras.Application.Local.App.Monetization;

/// <summary>Cấu hình app tải lúc khởi động (và cache lại). Người dùng VIP luôn nhận quảng cáo tắt.</summary>
public record AppRemoteConfigDto
{
    public int FreeEpisodes { get; init; }
    public int AdEpisodes { get; init; }
    public string? PrivacyUrl { get; init; }
    public string? TermsUrl { get; init; }

    public bool AdsEnabled { get; init; }
    public bool PurchasesEnabled { get; init; }
    public bool RequireAdVerification { get; init; }

    /// <summary>Tắt thì app không cần gửi thống kê quảng cáo.</summary>
    public bool AdStatsEnabled { get; init; } = true;
    public string PaywallVariant { get; init; } = "A";
    public int PaywallAfterRewardedAds { get; init; }
    public string? MinAppVersion { get; init; }

    public List<AppAdPlacementConfigDto> Placements { get; init; } = [];
    public List<AppPlanConfigDto> Plans { get; init; } = [];
}

/// <param name="Format"><c>rewarded</c>, <c>interstitial</c>, <c>banner</c>, <c>native</c> hoặc <c>app_open</c>.</param>
/// <param name="UnitId">Ad unit ID cho nền tảng của app đang hỏi.</param>
/// <param name="Screen">Mã màn hiện quảng cáo; <c>null</c> là mọi màn.</param>
/// <param name="Priority">Nhiều vị trí cùng loại, cùng màn: thử theo thứ tự này (số nhỏ trước).</param>
/// <param name="DisplaySeconds">Native: tự tắt sau bấy nhiêu giây; 0 là không tự tắt.</param>
/// <param name="EveryItems">Native: chèn sau mỗi bấy nhiêu mục của danh sách; 0 là một lần ở đầu.</param>
public record AppAdPlacementConfigDto(string Code, string Format, string UnitId, int DailyLimit, int CooldownSeconds, int EveryEpisodes,
    string? Screen, int Priority, int DisplaySeconds, int EveryItems);

/// <param name="ProductId">Product ID trên cửa hàng của nền tảng đang hỏi; app lấy giá thật từ cửa hàng theo ID này.</param>
public record AppPlanConfigDto(string Code, string Title, string? Description, string ProductId, int PeriodDays, int TrialDays, bool IsHighlighted);

public static class AppPlatform
{
    public const string Android = "android";
    public const string Ios = "ios";

    public static string Normalize(string? platform)
        => string.Equals(platform?.Trim(), Ios, StringComparison.OrdinalIgnoreCase) ? Ios : Android;
}
