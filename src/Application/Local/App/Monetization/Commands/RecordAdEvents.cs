using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.App.Monetization.Commands;

/// <summary>
/// Một dòng thống kê app đã gom: số sự kiện của một vị trí trên một màn kể từ lần gửi trước. <see cref="Revenue"/> là tổng
/// doanh thu AdMob báo (onPaidEvent) tính bằng đơn vị tiền tệ, không phải micros.
/// </summary>
public record AdEventItem
{
    public string? Placement { get; init; }
    public string? Screen { get; init; }
    public int Requests { get; init; }
    public int Filled { get; init; }
    public int Failed { get; init; }
    public int Impressions { get; init; }
    public int Clicks { get; init; }
    public int Rewarded { get; init; }
    public int Dismissed { get; init; }
    public long RevenueMicros { get; init; }
    public string? Currency { get; init; }
    public string? Error { get; init; }
}

public record RecordAdEventsCommand(string? Platform, List<AdEventItem>? Items) : IRequest;

public class RecordAdEventsCommandValidator : AbstractValidator<RecordAdEventsCommand>
{
    public RecordAdEventsCommandValidator()
    {
        RuleFor(x => x.Items).NotNull().Must(x => x!.Count <= 200).WithMessage("Mỗi lần gửi tối đa 200 dòng.");
    }
}

public class RecordAdEventsCommandHandler(IApplicationDbContext context, TimeProvider timeProvider) : IRequestHandler<RecordAdEventsCommand>
{
    // Chặn giá trị vô lý từ client lỗi hoặc bị giả mạo; một lô thật không bao giờ lớn thế này.
    private const int MaxCount = 100_000;

    public async Task Handle(RecordAdEventsCommand request, CancellationToken cancellationToken)
    {
        var settings = await context.SettingsAsync(cancellationToken);
        if (!settings.AdStatsEnabled || request.Items is not { Count: > 0 }) return;

        var platform = AppPlatform.Normalize(request.Platform);
        var day = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var formats = await context.AppAdPlacements.AsNoTracking().Select(x => new { x.Code, x.Format }).ToListAsync(cancellationToken);

        // Gộp các dòng trùng khoá trước khi ghi.
        var merged = new Dictionary<(string Placement, string Screen, string Currency), AdEventItem>();
        foreach (var item in request.Items)
        {
            var placement = Clean(item.Placement, 50);
            if (placement is null || formats.All(x => x.Code != placement)) continue; // vị trí lạ: bỏ
            var key = (placement, Clean(item.Screen, 50) ?? "unknown", (Clean(item.Currency, 3) ?? "USD").ToUpperInvariant());
            var next = item with
            {
                Requests = Clamp(item.Requests), Filled = Clamp(item.Filled), Failed = Clamp(item.Failed),
                Impressions = Clamp(item.Impressions), Clicks = Clamp(item.Clicks), Rewarded = Clamp(item.Rewarded),
                Dismissed = Clamp(item.Dismissed), RevenueMicros = Math.Clamp(item.RevenueMicros, 0, 100_000_000_000)
            };
            merged[key] = merged.TryGetValue(key, out var old)
                ? next with
                {
                    Requests = old.Requests + next.Requests, Filled = old.Filled + next.Filled, Failed = old.Failed + next.Failed,
                    Impressions = old.Impressions + next.Impressions, Clicks = old.Clicks + next.Clicks,
                    Rewarded = old.Rewarded + next.Rewarded, Dismissed = old.Dismissed + next.Dismissed,
                    RevenueMicros = old.RevenueMicros + next.RevenueMicros, Error = next.Error ?? old.Error
                }
                : next;
        }
        if (merged.Count == 0) return;

        var codes = merged.Keys.Select(x => x.Placement).Distinct().ToList();
        var rows = await context.AppAdStats
            .Where(x => x.Day == day && x.Platform == platform && codes.Contains(x.PlacementCode))
            .ToListAsync(cancellationToken);

        foreach (var ((placement, screen, currency), item) in merged)
        {
            var row = rows.FirstOrDefault(x => x.PlacementCode == placement && x.ScreenCode == screen && x.Currency == currency);
            if (row is null)
            {
                row = new AppAdStat
                {
                    Day = day, PlacementCode = placement, ScreenCode = screen, Platform = platform, Currency = currency,
                    Format = formats.First(x => x.Code == placement).Format
                };
                context.AppAdStats.Add(row);
                rows.Add(row);
            }

            row.Requests += item.Requests;
            row.Filled += item.Filled;
            row.Failed += item.Failed;
            row.Impressions += item.Impressions;
            row.Clicks += item.Clicks;
            row.Rewarded += item.Rewarded;
            row.Dismissed += item.Dismissed;
            row.RevenueMicros += item.RevenueMicros;
            if (Clean(item.Error, 200) is { } error) row.LastError = error;
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Hai máy gửi cùng lúc tạo dòng mới của ngày: client sẽ gửi lại lô này ở lần sau.
            throw new InvalidOperationException("Thống kê quảng cáo đang bận, thử lại sau.");
        }
    }

    private static int Clamp(int value) => Math.Clamp(value, 0, MaxCount);

    private static string? Clean(string? value, int max)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value) ? null : value.Length > max ? value[..max] : value;
    }
}
