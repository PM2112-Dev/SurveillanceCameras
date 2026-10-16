using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.App.Monetization;
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
        groupBuilder.MapPut(UpdateAdPlacement, "ad-placements/{id}");

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

    [EndpointSummary("Update ad placement")]
    [EndpointDescription("Sửa ad unit, bật/tắt và giới hạn của một vị trí quảng cáo")]
    public static async Task<Results<NoContent, BadRequest>> UpdateAdPlacement(ISender sender, int id, UpdateAdPlacementCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

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
