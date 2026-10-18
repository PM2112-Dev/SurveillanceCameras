using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.App.Monetization.Queries;

/// <param name="Platform"><c>android</c> hoặc <c>ios</c>: chọn ad unit và product ID của nền tảng đó.</param>
public record GetAppRemoteConfigQuery(string? Platform) : IRequest<AppRemoteConfigDto>;

public class GetAppRemoteConfigQueryHandler : IRequestHandler<GetAppRemoteConfigQuery, AppRemoteConfigDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;
    private readonly AppOptions _options;

    public GetAppRemoteConfigQueryHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider,
        IOptions<AppOptions> options)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<AppRemoteConfigDto> Handle(GetAppRemoteConfigQuery request, CancellationToken cancellationToken)
    {
        var ios = AppPlatform.Normalize(request.Platform) == AppPlatform.Ios;
        var settings = await _context.SettingsAsync(cancellationToken);
        var isVip = await _context.IsVipAsync(_user.Id, _timeProvider.GetUtcNow(), cancellationToken);

        var disabledScreens = (await _context.AppAdScreens.AsNoTracking().Where(x => !x.IsEnabled).Select(x => x.Code).ToListAsync(cancellationToken))
            .ToHashSet();
        var placements = (await _context.PlacementsAsync(cancellationToken))
            .Where(x => x.IsEnabled && (x.ScreenCode == null || !disabledScreens.Contains(x.ScreenCode)))
            .Select(x => new { Placement = x, Unit = ios ? x.IosUnitId : x.AndroidUnitId })
            .Where(x => !string.IsNullOrWhiteSpace(x.Unit) && x.Placement.Code != null)
            .OrderBy(x => x.Placement.Priority).ThenBy(x => x.Placement.Code, StringComparer.Ordinal)
            .Select(x => new AppAdPlacementConfigDto(x.Placement.Code!, Monetization.FormatName(x.Placement.Format), x.Unit!.Trim(),
                x.Placement.DailyLimit, x.Placement.CooldownSeconds, x.Placement.EveryEpisodes,
                string.IsNullOrWhiteSpace(x.Placement.ScreenCode) ? null : x.Placement.ScreenCode, x.Placement.Priority,
                x.Placement.DisplaySeconds, x.Placement.EveryItems))
            .ToList();

        var plans = (await _context.AppSubscriptionPlans.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
                .ToListAsync(cancellationToken))
            .Select(x => new { Plan = x, ProductId = ios ? x.AppleProductId : x.GoogleProductId })
            .Where(x => !string.IsNullOrWhiteSpace(x.ProductId))
            .Select(x => new AppPlanConfigDto(x.Plan.Code ?? "", x.Plan.Title ?? x.Plan.Code ?? "", x.Plan.Description,
                x.ProductId!.Trim(), x.Plan.PeriodDays, x.Plan.TrialDays, x.Plan.IsHighlighted))
            .ToList();

        return new AppRemoteConfigDto
        {
            FreeEpisodes = _options.FreeEpisodes,
            AdEpisodes = _options.AdEpisodes,
            PrivacyUrl = _options.PrivacyUrl,
            TermsUrl = _options.TermsUrl,
            // VIP không thấy quảng cáo nào.
            AdsEnabled = settings.AdsEnabled && !isVip && placements.Count > 0,
            PurchasesEnabled = settings.PurchasesEnabled && plans.Count > 0,
            RequireAdVerification = settings.RequireAdVerification,
            AdStatsEnabled = settings.AdStatsEnabled,
            PaywallVariant = settings.PaywallVariant,
            PaywallAfterRewardedAds = settings.PaywallAfterRewardedAds,
            MinAppVersion = settings.MinAppVersion,
            Placements = isVip ? [] : placements,
            Plans = plans
        };
    }
}
