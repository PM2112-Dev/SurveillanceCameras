using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.App.Monetization;

// Trang quản trị: cấu hình kiếm tiền, gói VIP, vị trí quảng cáo, thống kê.

public record MonetizationSettingsDto(bool AdsEnabled, bool PurchasesEnabled, bool RequireAdVerification, string PaywallVariant,
    int PaywallAfterRewardedAds, string? MinAppVersion);

public record GetMonetizationSettingsQuery : IRequest<MonetizationSettingsDto>;

public class GetMonetizationSettingsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetMonetizationSettingsQuery, MonetizationSettingsDto>
{
    public async Task<MonetizationSettingsDto> Handle(GetMonetizationSettingsQuery request, CancellationToken cancellationToken)
    {
        var s = await context.SettingsAsync(cancellationToken);
        return new MonetizationSettingsDto(s.AdsEnabled, s.PurchasesEnabled, s.RequireAdVerification, s.PaywallVariant,
            s.PaywallAfterRewardedAds, s.MinAppVersion);
    }
}

public record UpdateMonetizationSettingsCommand(bool AdsEnabled, bool PurchasesEnabled, bool RequireAdVerification,
    string? PaywallVariant, int PaywallAfterRewardedAds, string? MinAppVersion) : IRequest;

public class UpdateMonetizationSettingsCommandValidator : AbstractValidator<UpdateMonetizationSettingsCommand>
{
    public UpdateMonetizationSettingsCommandValidator()
    {
        RuleFor(x => x.PaywallVariant).Must(x => x is null or "A" or "B" or "C" or "D").WithMessage("Mẫu màn mua gói là A, B, C hoặc D.");
        RuleFor(x => x.PaywallAfterRewardedAds).InclusiveBetween(0, 100);
        RuleFor(x => x.MinAppVersion).Matches(@"^\d+(\.\d+){0,3}$").When(x => !string.IsNullOrWhiteSpace(x.MinAppVersion))
            .WithMessage("Phiên bản dạng 1.2.0.");
    }
}

public class UpdateMonetizationSettingsCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateMonetizationSettingsCommand>
{
    public async Task Handle(UpdateMonetizationSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await context.AppMonetizationSettings.OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new AppMonetizationSettings();
            context.AppMonetizationSettings.Add(settings);
        }

        settings.AdsEnabled = request.AdsEnabled;
        settings.PurchasesEnabled = request.PurchasesEnabled;
        settings.RequireAdVerification = request.RequireAdVerification;
        settings.PaywallVariant = request.PaywallVariant ?? "A";
        settings.PaywallAfterRewardedAds = request.PaywallAfterRewardedAds;
        settings.MinAppVersion = string.IsNullOrWhiteSpace(request.MinAppVersion) ? null : request.MinAppVersion.Trim();
        await context.SaveChangesAsync(cancellationToken);
    }
}

public record SubscriptionPlanDto(int Id, string Code, string? Title, string? Description, string? AppleProductId,
    string? GoogleProductId, int PeriodDays, int TrialDays, bool IsHighlighted, bool IsActive, int SortOrder);

public record GetSubscriptionPlansQuery : IRequest<List<SubscriptionPlanDto>>;

public class GetSubscriptionPlansQueryHandler(IApplicationDbContext context) : IRequestHandler<GetSubscriptionPlansQuery, List<SubscriptionPlanDto>>
{
    public Task<List<SubscriptionPlanDto>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
        => context.AppSubscriptionPlans.AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new SubscriptionPlanDto(x.Id, x.Code ?? "", x.Title, x.Description, x.AppleProductId, x.GoogleProductId,
                x.PeriodDays, x.TrialDays, x.IsHighlighted, x.IsActive, x.SortOrder))
            .ToListAsync(cancellationToken);
}

/// <summary>Thêm (<see cref="Id"/> = 0) hoặc sửa một gói VIP.</summary>
public record SaveSubscriptionPlanCommand : IRequest<int>
{
    public int Id { get; init; }
    public string? Code { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? AppleProductId { get; init; }
    public string? GoogleProductId { get; init; }
    public int PeriodDays { get; init; }
    public int TrialDays { get; init; }
    public bool IsHighlighted { get; init; }
    public bool IsActive { get; init; } = true;
    public int SortOrder { get; init; }
}

public class SaveSubscriptionPlanCommandValidator : AbstractValidator<SaveSubscriptionPlanCommand>
{
    public SaveSubscriptionPlanCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[a-z0-9_-]+$").WithMessage("Mã gói chỉ gồm chữ thường, số, - và _.")
            .MustAsync(async (command, code, ct) => !await context.AppSubscriptionPlans.AnyAsync(x => x.Code == code && x.Id != command.Id, ct))
            .WithMessage("Mã gói đã tồn tại.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(200);
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.AppleProductId) || !string.IsNullOrWhiteSpace(x.GoogleProductId))
            .WithName("ProductId").WithMessage("Nhập product ID của App Store hoặc Google Play.");
        RuleFor(x => x.PeriodDays).GreaterThan(0);
        RuleFor(x => x.TrialDays).GreaterThanOrEqualTo(0);
    }
}

public class SaveSubscriptionPlanCommandHandler(IApplicationDbContext context) : IRequestHandler<SaveSubscriptionPlanCommand, int>
{
    public async Task<int> Handle(SaveSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        AppSubscriptionPlan plan;
        if (request.Id == 0)
        {
            plan = new AppSubscriptionPlan();
            context.AppSubscriptionPlans.Add(plan);
        }
        else
        {
            plan = await context.AppSubscriptionPlans.FindAsync([request.Id], cancellationToken)
                   ?? throw new NotFoundException(request.Id.ToString(), nameof(AppSubscriptionPlan));
        }

        plan.Code = request.Code!.Trim();
        plan.Title = request.Title?.Trim();
        plan.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        plan.AppleProductId = string.IsNullOrWhiteSpace(request.AppleProductId) ? null : request.AppleProductId.Trim();
        plan.GoogleProductId = string.IsNullOrWhiteSpace(request.GoogleProductId) ? null : request.GoogleProductId.Trim();
        plan.PeriodDays = request.PeriodDays;
        plan.TrialDays = request.TrialDays;
        plan.IsHighlighted = request.IsHighlighted;
        plan.IsActive = request.IsActive;
        plan.SortOrder = request.SortOrder;

        // Chỉ một gói được làm nổi bật.
        if (plan.IsHighlighted)
            foreach (var other in await context.AppSubscriptionPlans.Where(x => x.IsHighlighted && x.Id != plan.Id).ToListAsync(cancellationToken))
                other.IsHighlighted = false;

        await context.SaveChangesAsync(cancellationToken);
        return plan.Id;
    }
}

public record DeleteSubscriptionPlanCommand(int Id) : IRequest;

public class DeleteSubscriptionPlanCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteSubscriptionPlanCommand>
{
    public async Task Handle(DeleteSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await context.AppSubscriptionPlans.FindAsync([request.Id], cancellationToken);
        if (plan is null) return;
        // Gói đã bán vẫn giữ mã trong AppSubscription.Plan; chỉ ngừng hiện trong app.
        context.AppSubscriptionPlans.Remove(plan);
        await context.SaveChangesAsync(cancellationToken);
    }
}

public record AdPlacementDto(int Id, string Code, string? Title, string Format, string? AndroidUnitId, string? IosUnitId,
    bool IsEnabled, int DailyLimit, int CooldownSeconds, int EveryEpisodes);

/// <summary>Các vị trí quảng cáo; lần đầu mở trang thì tạo bộ mặc định (ad unit thử nghiệm của Google).</summary>
public record GetAdPlacementsQuery : IRequest<List<AdPlacementDto>>;

public class GetAdPlacementsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetAdPlacementsQuery, List<AdPlacementDto>>
{
    public async Task<List<AdPlacementDto>> Handle(GetAdPlacementsQuery request, CancellationToken cancellationToken)
    {
        var existing = await context.AppAdPlacements.Select(x => x.Code).ToListAsync(cancellationToken);
        var missing = Monetization.DefaultPlacements.Where(x => !existing.Contains(x.Code)).ToList();
        if (missing.Count > 0)
        {
            context.AppAdPlacements.AddRange(missing.Select(x => new AppAdPlacement
            {
                Code = x.Code, Title = x.Title, Format = x.Format, AndroidUnitId = x.AndroidUnitId, IosUnitId = x.IosUnitId,
                IsEnabled = x.IsEnabled, DailyLimit = x.DailyLimit, CooldownSeconds = x.CooldownSeconds, EveryEpisodes = x.EveryEpisodes
            }));
            await context.SaveChangesAsync(cancellationToken);
        }

        return (await context.AppAdPlacements.AsNoTracking().OrderBy(x => x.Format).ThenBy(x => x.Id).ToListAsync(cancellationToken))
            .Select(x => new AdPlacementDto(x.Id, x.Code ?? "", x.Title, Monetization.FormatName(x.Format), x.AndroidUnitId, x.IosUnitId,
                x.IsEnabled, x.DailyLimit, x.CooldownSeconds, x.EveryEpisodes))
            .ToList();
    }
}

public record UpdateAdPlacementCommand(int Id, string? Title, string? AndroidUnitId, string? IosUnitId, bool IsEnabled,
    int DailyLimit, int CooldownSeconds, int EveryEpisodes) : IRequest;

public class UpdateAdPlacementCommandValidator : AbstractValidator<UpdateAdPlacementCommand>
{
    private const string UnitPattern = @"^ca-app-pub-\d+/\d+$";

    public UpdateAdPlacementCommandValidator()
    {
        RuleFor(x => x.AndroidUnitId).Matches(UnitPattern).When(x => !string.IsNullOrWhiteSpace(x.AndroidUnitId))
            .WithMessage("Ad unit ID dạng ca-app-pub-1234567890123456/1234567890.");
        RuleFor(x => x.IosUnitId).Matches(UnitPattern).When(x => !string.IsNullOrWhiteSpace(x.IosUnitId))
            .WithMessage("Ad unit ID dạng ca-app-pub-1234567890123456/1234567890.");
        RuleFor(x => x.DailyLimit).InclusiveBetween(0, 1000);
        RuleFor(x => x.CooldownSeconds).InclusiveBetween(0, 86400);
        RuleFor(x => x.EveryEpisodes).InclusiveBetween(0, 100);
    }
}

public class UpdateAdPlacementCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateAdPlacementCommand>
{
    public async Task Handle(UpdateAdPlacementCommand request, CancellationToken cancellationToken)
    {
        var placement = await context.AppAdPlacements.FindAsync([request.Id], cancellationToken)
                        ?? throw new NotFoundException(request.Id.ToString(), nameof(AppAdPlacement));
        placement.Title = string.IsNullOrWhiteSpace(request.Title) ? placement.Title : request.Title.Trim();
        placement.AndroidUnitId = string.IsNullOrWhiteSpace(request.AndroidUnitId) ? null : request.AndroidUnitId.Trim();
        placement.IosUnitId = string.IsNullOrWhiteSpace(request.IosUnitId) ? null : request.IosUnitId.Trim();
        placement.IsEnabled = request.IsEnabled;
        placement.DailyLimit = request.DailyLimit;
        placement.CooldownSeconds = request.CooldownSeconds;
        placement.EveryEpisodes = request.EveryEpisodes;
        await context.SaveChangesAsync(cancellationToken);
    }
}

/// <param name="RewardedVerified">Lượt AdMob xác nhận (SSV) trong ngày.</param>
/// <param name="EpisodesUnlocked">Số tập mở bằng quảng cáo trong ngày.</param>
/// <param name="NewSubscriptions">Gói mua qua cửa hàng bắt đầu trong ngày.</param>
public record MonetizationDayDto(DateOnly Day, int RewardedVerified, int EpisodesUnlocked, int NewSubscriptions);

public record MonetizationStatsDto(int ActiveVip, int ActiveStoreSubscriptions, int ActiveAdminGrants, List<MonetizationDayDto> Days);

public record GetMonetizationStatsQuery(int Days = 30) : IRequest<MonetizationStatsDto>;

public class GetMonetizationStatsQueryHandler(IApplicationDbContext context, TimeProvider timeProvider)
    : IRequestHandler<GetMonetizationStatsQuery, MonetizationStatsDto>
{
    public async Task<MonetizationStatsDto> Handle(GetMonetizationStatsQuery request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var days = Math.Clamp(request.Days, 1, 180);
        var from = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1 - days), TimeSpan.Zero);

        var active = context.AppSubscriptions.AsNoTracking().Where(x => x.ExpiresAt > now);
        var activeVip = await active.Select(x => x.UserId).Distinct().CountAsync(cancellationToken);
        var store = await active.CountAsync(x => x.Source != SubscriptionSource.Admin, cancellationToken);
        var admin = await active.CountAsync(x => x.Source == SubscriptionSource.Admin, cancellationToken);

        var rewards = await context.AppAdRewards.AsNoTracking().Where(x => x.RewardedAt >= from)
            .Select(x => x.RewardedAt).ToListAsync(cancellationToken);
        var unlocks = await context.AppEpisodeUnlocks.AsNoTracking().Where(x => x.Created >= from)
            .Select(x => x.Created).ToListAsync(cancellationToken);
        var subscriptions = await context.AppSubscriptions.AsNoTracking()
            .Where(x => x.Source != SubscriptionSource.Admin && x.Created >= from)
            .Select(x => x.Created).ToListAsync(cancellationToken);

        static DateOnly Day(DateTimeOffset at) => DateOnly.FromDateTime(at.UtcDateTime);
        var list = Enumerable.Range(0, days)
            .Select(i => DateOnly.FromDateTime(from.UtcDateTime.AddDays(i)))
            .Select(day => new MonetizationDayDto(day, rewards.Count(x => Day(x) == day), unlocks.Count(x => Day(x) == day),
                subscriptions.Count(x => Day(x) == day)))
            .ToList();

        return new MonetizationStatsDto(activeVip, store, admin, list);
    }
}
