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

        var placements = (await _context.PlacementsAsync(cancellationToken))
            .Where(x => x.IsEnabled)
            .Select(x => new { x.Code, x.Format, x.DailyLimit, x.CooldownSeconds, x.EveryEpisodes, Unit = ios ? x.IosUnitId : x.AndroidUnitId })
            .Where(x => !string.IsNullOrWhiteSpace(x.Unit) && x.Code != null)
            .OrderBy(x => x.Code)
            .Select(x => new AppAdPlacementConfigDto(x.Code!, Monetization.FormatName(x.Format), x.Unit!.Trim(), x.DailyLimit,
                x.CooldownSeconds, x.EveryEpisodes))
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
            PaywallVariant = settings.PaywallVariant,
            PaywallAfterRewardedAds = settings.PaywallAfterRewardedAds,
            MinAppVersion = settings.MinAppVersion,
            Placements = isVip ? [] : placements,
            Plans = plans
        };
    }
}
