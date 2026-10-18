using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.App.Monetization.Queries;

/// <param name="Revenue">Doanh thu theo tiền tệ, đơn vị micros (1/1.000.000).</param>
public record AdStatTotalsDto(long Requests, long Filled, long Failed, long Impressions, long Clicks, long Rewarded, long Dismissed,
    Dictionary<string, long> Revenue);

/// <param name="Key">Khoá nhóm: ngày (yyyy-MM-dd), mã vị trí, mã màn, nền tảng hoặc loại.</param>
public record AdStatRowDto(string Key, string? Label, string? Format, AdStatTotalsDto Totals, string? LastError);

public record AdStatsDto(
    DateOnly From,
    DateOnly To,
    AdStatTotalsDto Totals,
    /// <summary>Cùng độ dài khoảng ngay trước đó, để so sánh tăng giảm.</summary>
    AdStatTotalsDto Previous,
    List<AdStatRowDto> ByDay,
    List<AdStatRowDto> ByPlacement,
    List<AdStatRowDto> ByScreen,
    List<AdStatRowDto> ByPlatform,
    List<AdStatRowDto> ByFormat);

/// <summary>Thống kê quảng cáo trong khoảng ngày (UTC), lọc theo nền tảng, màn, vị trí, loại.</summary>
public record GetAdStatsQuery(DateOnly? From, DateOnly? To, string? Platform, string? Screen, string? Placement, string? Format)
    : IRequest<AdStatsDto>;

public class GetAdStatsQueryHandler(IApplicationDbContext context, TimeProvider timeProvider) : IRequestHandler<GetAdStatsQuery, AdStatsDto>
{
    public async Task<AdStatsDto> Handle(GetAdStatsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var to = request.To ?? today;
        var from = request.From ?? to.AddDays(-29);
        if (from > to) (from, to) = (to, from);
        if (to.DayNumber - from.DayNumber > 365) from = to.AddDays(-365);
        var length = to.DayNumber - from.DayNumber + 1;
        var previousFrom = from.AddDays(-length);

        var query = context.AppAdStats.AsNoTracking().Where(x => x.Day >= previousFrom && x.Day <= to);
        if (!string.IsNullOrWhiteSpace(request.Platform)) { var p = AppPlatform.Normalize(request.Platform); query = query.Where(x => x.Platform == p); }
        if (!string.IsNullOrWhiteSpace(request.Screen)) query = query.Where(x => x.ScreenCode == request.Screen);
        if (!string.IsNullOrWhiteSpace(request.Placement)) query = query.Where(x => x.PlacementCode == request.Placement);
        if (Monetization.ParseFormat(request.Format) is { } format) query = query.Where(x => x.Format == format);

        // Mỗi dòng là một tổ hợp ngày/vị trí/màn/nền tảng/tiền tệ nên số dòng nhỏ.
        var rows = await query.ToListAsync(cancellationToken);
        var current = rows.Where(x => x.Day >= from).ToList();
        var previous = rows.Where(x => x.Day < from).ToList();

        var titles = await context.AppAdPlacements.AsNoTracking().Select(x => new { x.Code, x.Title }).ToListAsync(cancellationToken);
        var screens = await context.AppAdScreens.AsNoTracking().Select(x => new { x.Code, x.Title }).ToListAsync(cancellationToken);

        List<AdStatRowDto> Group(Func<Domain.Entities.AppAdStat, string> key, Func<string, string?> label,
            Func<IGrouping<string, Domain.Entities.AppAdStat>, string?>? format = null)
            => current.GroupBy(key)
                .Select(g => new AdStatRowDto(g.Key, label(g.Key), format?.Invoke(g), Sum(g),
                    g.OrderByDescending(x => x.LastModified).Select(x => x.LastError).FirstOrDefault(x => x != null)))
                .ToList();

        var byDay = Enumerable.Range(0, length)
            .Select(i => from.AddDays(i))
            .Select(day => new AdStatRowDto(day.ToString("yyyy-MM-dd"), null, null, Sum(current.Where(x => x.Day == day)), null))
            .ToList();

        return new AdStatsDto(from, to, Sum(current), Sum(previous), byDay,
            Group(x => x.PlacementCode, key => titles.FirstOrDefault(t => t.Code == key)?.Title,
                    g => Monetization.FormatName(g.First().Format))
                .OrderByDescending(x => x.Totals.Impressions).ThenByDescending(x => x.Totals.Requests).ToList(),
            Group(x => x.ScreenCode, key => screens.FirstOrDefault(t => t.Code == key)?.Title)
                .OrderByDescending(x => x.Totals.Impressions).ThenByDescending(x => x.Totals.Requests).ToList(),
            Group(x => x.Platform, _ => null).OrderBy(x => x.Key).ToList(),
            Group(x => Monetization.FormatName(x.Format), _ => null).OrderBy(x => x.Key).ToList());
    }

    private static AdStatTotalsDto Sum(IEnumerable<Domain.Entities.AppAdStat> rows)
    {
        long requests = 0, filled = 0, failed = 0, impressions = 0, clicks = 0, rewarded = 0, dismissed = 0;
        var revenue = new Dictionary<string, long>();
        foreach (var x in rows)
        {
            requests += x.Requests; filled += x.Filled; failed += x.Failed; impressions += x.Impressions;
            clicks += x.Clicks; rewarded += x.Rewarded; dismissed += x.Dismissed;
            if (x.RevenueMicros > 0) revenue[x.Currency] = revenue.GetValueOrDefault(x.Currency) + x.RevenueMicros;
        }
        return new AdStatTotalsDto(requests, filled, failed, impressions, clicks, rewarded, dismissed, revenue);
    }
}
