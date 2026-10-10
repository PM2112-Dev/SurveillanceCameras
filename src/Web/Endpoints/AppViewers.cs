using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using SurveillanceCameras.Application.Local.App.Commands;
using SurveillanceCameras.Domain.Constants;
using SurveillanceCameras.Infrastructure.Identity;

namespace SurveillanceCameras.Web.Endpoints;

/// <summary>Quản trị người xem của app (chỉ Administrator).</summary>
public class AppViewers : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPut(SetAppViewerVip, "vip");
    }

    /// <param name="ExpiresAt">Hạn VIP; thời điểm đã qua nghĩa là thu hồi.</param>
    public sealed record SetAppViewerVipRequest(string? Email, string? Plan, DateTimeOffset ExpiresAt);

    [EndpointSummary("Set app viewer VIP")]
    [EndpointDescription("Cấp/gia hạn/thu hồi VIP cho người xem theo email — dùng khi chưa nối thanh toán của cửa hàng hoặc để hỗ trợ khách")]
    public static async Task<Results<NoContent, NotFound>> SetAppViewerVip(ISender sender,
        UserManager<ApplicationUser> userManager, SetAppViewerVipRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email?.Trim() ?? "");
        if (user is null) return TypedResults.NotFound();

        await sender.Send(new SetAppViewerVipCommand(user.Id, request.Plan, request.ExpiresAt));
        return TypedResults.NoContent();
    }
}
