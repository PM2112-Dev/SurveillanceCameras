using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Local.App.Monetization;
using SurveillanceCameras.Application.Local.App.Monetization.Commands;

namespace SurveillanceCameras.Web.Endpoints;

/// <summary>
/// Địa chỉ App Store, Google Play và AdMob gọi về máy chủ (không đăng nhập; mỗi nơi tự chứng thực bằng chữ ký hoặc mã bí mật).
/// </summary>
public class AppStore : IEndpointGroup
{
    public static string? RoutePrefix => "/api/app/store";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.AllowAnonymous();

        groupBuilder.MapPost(AppStoreNotification, "app-store/notifications");
        groupBuilder.MapPost(GooglePlayNotification, "google-play/notifications");
        groupBuilder.MapGet(AdMobRewardCallback, "admob/ssv");
    }

    public sealed record AppStoreNotificationRequest(string? SignedPayload);

    public sealed record PubSubMessage(string? MessageId, string? Data);

    public sealed record PubSubPushRequest(PubSubMessage? Message, string? Subscription);

    [EndpointSummary("App Store server notification")]
    [EndpointDescription("App Store Server Notifications V2 (khai báo URL này trên App Store Connect). Thông báo đã xử lý thì bỏ qua")]
    public static async Task<Ok> AppStoreNotification(ISender sender, AppStoreNotificationRequest request)
    {
        await sender.Send(new HandleAppStoreNotificationCommand(request.SignedPayload));
        return TypedResults.Ok();
    }

    [EndpointSummary("Google Play real-time developer notification")]
    [EndpointDescription("Pub/Sub push của Google Play. URL push phải kèm ?token= trùng Store:GooglePlay:NotificationToken")]
    public static async Task<Results<Ok, UnauthorizedHttpResult>> GooglePlayNotification(ISender sender, IGooglePlayClient googlePlay,
        PubSubPushRequest request, string? token = null)
    {
        if (!googlePlay.IsNotificationTokenValid(token)) return TypedResults.Unauthorized();

        await sender.Send(new HandleGooglePlayNotificationCommand(request.Message?.MessageId, request.Message?.Data));
        return TypedResults.Ok();
    }

    [EndpointSummary("AdMob rewarded ad verification")]
    [EndpointDescription("Callback server-side verification của quảng cáo có thưởng AdMob (khai báo URL này cho ad unit trên AdMob)")]
    public static async Task<Ok> AdMobRewardCallback(ISender sender, HttpContext httpContext)
    {
        await sender.Send(new RecordAdRewardCommand(httpContext.Request.QueryString.Value));
        return TypedResults.Ok();
    }
}
