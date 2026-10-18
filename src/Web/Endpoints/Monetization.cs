using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.App.Monetization;
using SurveillanceCameras.Application.Local.App.Monetization.Queries;
using SurveillanceCameras.Domain.Constants;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Data;

namespace SurveillanceCameras.Web.Endpoints;

/// <summary>Quản trị quảng cáo và gói VIP của app (chỉ Administrator).</summary>
public class Monetization : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapGet(GetMonetizationSettings, "settings");
        groupBuilder.MapPut(UpdateMonetizationSettings, "settings");

        groupBuilder.MapGet(GetSubscriptionPlans, "plans");
        groupBuilder.MapPost(CreateSubscriptionPlan, "plans");
        groupBuilder.MapPut(UpdateSubscriptionPlan, "plans/{id}");
        groupBuilder.MapDelete(DeleteSubscriptionPlan, "plans/{id}");

        groupBuilder.MapGet(GetAdPlacements, "ad-placements");
        groupBuilder.MapPost(CreateAdPlacement, "ad-placements");
        groupBuilder.MapPut(UpdateAdPlacement, "ad-placements/{id}");
        groupBuilder.MapDelete(DeleteAdPlacement, "ad-placements/{id}");

        groupBuilder.MapGet(GetAdScreens, "ad-screens");
        groupBuilder.MapPost(CreateAdScreen, "ad-screens");
        groupBuilder.MapPut(UpdateAdScreen, "ad-screens/{id}");
        groupBuilder.MapDelete(DeleteAdScreen, "ad-screens/{id}");
        groupBuilder.MapGet(GetAdStats, "ad-stats");

        groupBuilder.MapGet(GetAppSubscriptions, "subscriptions");
        groupBuilder.MapGet(GetMonetizationStats, "stats");
    }

    [EndpointSummary("Get monetization settings")]
    [EndpointDescription("Công tắc quảng cáo/mua gói, bắt buộc xác minh quảng cáo, mẫu màn mua gói, phiên bản app tối thiểu")]
    public static async Task<Ok<MonetizationSettingsDto>> GetMonetizationSettings(ISender sender)
        => TypedResults.Ok(await sender.Send(new GetMonetizationSettingsQuery()));

    [EndpointSummary("Update monetization settings")]
    [EndpointDescription("Lưu cấu hình kiếm tiền; app nhận ở lần tải cấu hình tiếp theo")]
    public static async Task<NoContent> UpdateMonetizationSettings(ISender sender, UpdateMonetizationSettingsCommand command)
    {
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Get subscription plans")]
    [EndpointDescription("Các gói VIP bán trong app")]
    public static async Task<Ok<List<SubscriptionPlanDto>>> GetSubscriptionPlans(ISender sender)
        => TypedResults.Ok(await sender.Send(new GetSubscriptionPlansQuery()));

    [EndpointSummary("Create subscription plan")]
    [EndpointDescription("Thêm gói VIP (product ID phải trùng gói đã tạo trên App Store Connect / Google Play Console)")]
    public static async Task<Created<int>> CreateSubscriptionPlan(ISender sender, SaveSubscriptionPlanCommand command)
    {
        var id = await sender.Send(command with { Id = 0 });
        return TypedResults.Created($"/api/{nameof(Monetization)}/plans/{id}", id);
    }

    [EndpointSummary("Update subscription plan")]
    [EndpointDescription("Sửa gói VIP")]
    public static async Task<Results<NoContent, BadRequest>> UpdateSubscriptionPlan(ISender sender, int id, SaveSubscriptionPlanCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete subscription plan")]
    [EndpointDescription("Xoá gói khỏi danh sách bán; người đã mua vẫn giữ quyền tới hết hạn")]
    public static async Task<NoContent> DeleteSubscriptionPlan(ISender sender, int id)
    {
        await sender.Send(new DeleteSubscriptionPlanCommand(id));
        return TypedResults.NoContent();
    }

    [EndpointSummary("Get ad placements")]
    [EndpointDescription("Các vị trí quảng cáo AdMob trong app")]
    public static async Task<Ok<List<AdPlacementDto>>> GetAdPlacements(ISender sender)
        => TypedResults.Ok(await sender.Send(new GetAdPlacementsQuery()));

    [EndpointSummary("Create ad placement")]
    [EndpointDescription("Thêm vị trí quảng cáo (rewarded, interstitial, banner, native, app_open); được nhiều vị trí cùng loại, cùng màn")]
    public static async Task<Created<int>> CreateAdPlacement(ISender sender, SaveAdPlacementCommand command)
    {
        var id = await sender.Send(command with { Id = 0 });
        return TypedResults.Created($"/api/{nameof(Monetization)}/ad-placements/{id}", id);
    }

    [EndpointSummary("Update ad placement")]
    [EndpointDescription("Sửa ad unit, màn, thứ tự, bật/tắt và giới hạn của một vị trí quảng cáo")]
    public static async Task<Results<NoContent, BadRequest>> UpdateAdPlacement(ISender sender, int id, SaveAdPlacementCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete ad placement")]
    [EndpointDescription("Xoá vị trí quảng cáo tự thêm (vị trí có sẵn của app chỉ tắt được); thống kê cũ vẫn giữ")]
    public static async Task<NoContent> DeleteAdPlacement(ISender sender, int id)
    {
        await sender.Send(new DeleteAdPlacementCommand(id));
        return TypedResults.NoContent();
    }

    [EndpointSummary("Get ad screens")]
    [EndpointDescription("Các màn hình của app đã khai báo để gắn quảng cáo và thống kê")]
    public static async Task<Ok<List<AdScreenDto>>> GetAdScreens(ISender sender)
        => TypedResults.Ok(await sender.Send(new GetAdScreensQuery()));

    [EndpointSummary("Create ad screen")]
    [EndpointDescription("Khai báo màn hình mới (mã phải trùng mã app dùng khi báo thống kê)")]
    public static async Task<Created<int>> CreateAdScreen(ISender sender, SaveAdScreenCommand command)
    {
        var id = await sender.Send(command with { Id = 0 });
        return TypedResults.Created($"/api/{nameof(Monetization)}/ad-screens/{id}", id);
    }

    [EndpointSummary("Update ad screen")]
    [EndpointDescription("Đổi tên, bật/tắt hoặc thứ tự của màn; tắt màn thì quảng cáo của màn đó ngừng hiện")]
    public static async Task<Results<NoContent, BadRequest>> UpdateAdScreen(ISender sender, int id, SaveAdScreenCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete ad screen")]
    [EndpointDescription("Xoá màn tự khai báo, khi không còn vị trí quảng cáo gắn với màn")]
    public static async Task<NoContent> DeleteAdScreen(ISender sender, int id)
    {
        await sender.Send(new DeleteAdScreenCommand(id));
        return TypedResults.NoContent();
    }

    [EndpointSummary("Get ad stats")]
    [EndpointDescription("Thống kê quảng cáo: request, lấp đầy, hiển thị, bấm, thưởng, doanh thu; theo ngày, vị trí, màn, nền tảng, loại. " +
                         "Lọc theo khoảng ngày (UTC), nền tảng, màn, vị trí, loại; kèm kỳ trước để so sánh")]
    public static async Task<Ok<AdStatsDto>> GetAdStats(ISender sender, DateTimeOffset? from = null, DateTimeOffset? to = null,
        string? platform = null, string? screen = null, string? placement = null, string? format = null)
        => TypedResults.Ok(await sender.Send(new GetAdStatsQuery(
            from is null ? null : DateOnly.FromDateTime(from.Value.UtcDateTime),
            to is null ? null : DateOnly.FromDateTime(to.Value.UtcDateTime), platform, screen, placement, format)));

    public sealed record AppSubscriptionDto(int Id, string UserId, string? Email, string? Plan, string Source, string Status,
        string? ProductId, DateTimeOffset ExpiresAt, bool IsActive, bool AutoRenew, bool IsSandbox, DateTimeOffset Created,
        DateTimeOffset? VerifiedAt);

    [EndpointSummary("Get app subscriptions")]
    [EndpointDescription("Gói VIP của người xem (cấp tay và mua qua cửa hàng), mới nhất trước. Lọc theo email, nguồn, còn hiệu lực")]
    public static async Task<Ok<PaginatedList<AppSubscriptionDto>>> GetAppSubscriptions(ApplicationDbContext dbContext,
        TimeProvider timeProvider, string? email = null, SubscriptionSource? source = null, bool? active = null, int page = 1,
        int pageSize = 20)
    {
        var now = timeProvider.GetUtcNow();
        var query = from s in dbContext.AppSubscriptions.AsNoTracking()
            join u in dbContext.Users.AsNoTracking() on s.UserId equals u.Id into users
            from u in users.DefaultIfEmpty()
            select new { s, Email = u != null ? u.Email : null };

        if (!string.IsNullOrWhiteSpace(email))
        {
            var term = email.Trim().ToLower();
            query = query.Where(x => x.Email != null && x.Email.ToLower().Contains(term));
        }
        if (source is not null) query = query.Where(x => x.s.Source == source);
        if (active is not null) query = query.Where(x => (x.s.ExpiresAt > now) == active);

        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.s.LastModified)
            .Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var items = rows.Select(x => new AppSubscriptionDto(x.s.Id, x.s.UserId, x.Email, x.s.Plan, x.s.Source.ToString(),
            x.s.Status.ToString(), x.s.ProductId, x.s.ExpiresAt, x.s.ExpiresAt > now, x.s.AutoRenew, x.s.IsSandbox, x.s.Created,
            x.s.VerifiedAt)).ToList();
        return TypedResults.Ok(new PaginatedList<AppSubscriptionDto>(items, total, Math.Max(page, 1), pageSize));
    }

    [EndpointSummary("Get monetization stats")]
    [EndpointDescription("Số người đang VIP, gói đang hiệu lực theo nguồn, và theo ngày: lượt quảng cáo xác minh, tập mở bằng quảng cáo, gói mới")]
    public static async Task<Ok<MonetizationStatsDto>> GetMonetizationStats(ISender sender, int days = 30)
        => TypedResults.Ok(await sender.Send(new GetMonetizationStatsQuery(days)));
}
