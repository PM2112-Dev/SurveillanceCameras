using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.App;
using SurveillanceCameras.Application.Local.App.Commands;
using SurveillanceCameras.Application.Local.App.Monetization.Commands;
using SurveillanceCameras.Application.Local.App.Queries;
using SurveillanceCameras.Domain.Constants;
using SurveillanceCameras.Infrastructure.Data;
using SurveillanceCameras.Infrastructure.Identity;

namespace SurveillanceCameras.Web.Endpoints;

/// <summary>Hồ sơ và thư viện (phim đã lưu, lịch sử xem) của người xem đang đăng nhập trên app.</summary>
public class AppMe : IEndpointGroup
{
    public static string? RoutePrefix => "/api/app/me";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetAppProfile);
        groupBuilder.MapPut(UpdateAppProfile, "");
        groupBuilder.MapDelete(DeleteAppAccount, "");
        groupBuilder.MapPost(LogoutAppViewer, "logout");

        groupBuilder.MapPost(VerifyAppStorePurchase, "subscriptions/app-store");
        groupBuilder.MapPost(VerifyGooglePlayPurchase, "subscriptions/google-play");

        groupBuilder.MapGet(GetAppBookmarks, "bookmarks");
        groupBuilder.MapPut(AddAppBookmark, "bookmarks/{movieId}");
        groupBuilder.MapDelete(RemoveAppBookmark, "bookmarks/{movieId}");

        groupBuilder.MapGet(GetAppHistory, "history");
        groupBuilder.MapPut(SaveAppWatchProgress, "history/{movieId}");
        groupBuilder.MapDelete(RemoveAppHistory, "history/{movieId}");
    }

    public sealed record UpdateAppProfileRequest(string? DisplayName);

    /// <param name="SignedTransactions">Chuỗi JWS của giao dịch StoreKit 2 (vừa mua, hoặc mọi gói đang có khi khôi phục).</param>
    public sealed record AppStorePurchaseRequest(List<string>? SignedTransactions);

    /// <param name="PurchaseTokens">Purchase token Google Play (vừa mua, hoặc mọi gói đang có khi khôi phục).</param>
    public sealed record GooglePlayPurchaseRequest(List<string>? PurchaseTokens);

    [EndpointSummary("Verify App Store purchase")]
    [EndpointDescription("Kiểm chữ ký giao dịch StoreKit 2 rồi gắn gói VIP vào người đang đăng nhập. Trả hồ sơ mới (kèm hạn VIP)")]
    public static async Task<Results<Ok<AppAuth.AppProfileDto>, UnauthorizedHttpResult>> VerifyAppStorePurchase(ISender sender,
        IUser currentUser, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, TimeProvider timeProvider,
        AppStorePurchaseRequest request)
    {
        await sender.Send(new VerifyAppStorePurchaseCommand(request.SignedTransactions ?? []));
        return await ProfileResultAsync(currentUser, userManager, dbContext, timeProvider);
    }

    [EndpointSummary("Verify Google Play purchase")]
    [EndpointDescription("Hỏi Google Play trạng thái gói theo purchase token rồi gắn vào người đang đăng nhập. Trả hồ sơ mới (kèm hạn VIP)")]
    public static async Task<Results<Ok<AppAuth.AppProfileDto>, UnauthorizedHttpResult>> VerifyGooglePlayPurchase(ISender sender,
        IUser currentUser, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, TimeProvider timeProvider,
        GooglePlayPurchaseRequest request)
    {
        await sender.Send(new VerifyGooglePlayPurchaseCommand(request.PurchaseTokens ?? []));
        return await ProfileResultAsync(currentUser, userManager, dbContext, timeProvider);
    }

    private static async Task<Results<Ok<AppAuth.AppProfileDto>, UnauthorizedHttpResult>> ProfileResultAsync(IUser currentUser,
        UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        var user = await userManager.FindByIdAsync(currentUser.Id ?? "");
        return user is null ? TypedResults.Unauthorized() : TypedResults.Ok(await AppAuth.ProfileAsync(user, dbContext, timeProvider));
    }

    public sealed record SaveAppWatchProgressRequest(int EpisodeNumber, int PositionSeconds, int DurationSeconds,
        DateTimeOffset? WatchedAt);

    [EndpointSummary("Get app profile")]
    [EndpointDescription("Hồ sơ người xem đang đăng nhập kèm trạng thái VIP")]
    public static async Task<Results<Ok<AppAuth.AppProfileDto>, UnauthorizedHttpResult>> GetAppProfile(
        IUser currentUser, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        var user = await userManager.FindByIdAsync(currentUser.Id ?? "");
        if (user is null) return TypedResults.Unauthorized();

        return TypedResults.Ok(await AppAuth.ProfileAsync(user, dbContext, timeProvider));
    }

    [EndpointSummary("Update app profile")]
    [EndpointDescription("Đổi tên hiển thị của người xem")]
    public static async Task<Results<Ok<AppAuth.AppProfileDto>, ValidationProblem, UnauthorizedHttpResult>> UpdateAppProfile(
        IUser currentUser, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, TimeProvider timeProvider,
        UpdateAppProfileRequest request)
    {
        var user = await userManager.FindByIdAsync(currentUser.Id ?? "");
        if (user is null) return TypedResults.Unauthorized();

        var displayName = AppAuth.CleanDisplayName(request.DisplayName);
        if (displayName is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["DisplayName"] = ["Tên hiển thị không được để trống."]
            });
        }

        user.DisplayName = displayName;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return TypedResults.ValidationProblem(Users.ToValidationErrors(result));

        return TypedResults.Ok(await AppAuth.ProfileAsync(user, dbContext, timeProvider));
    }

    [EndpointSummary("Delete app account")]
    [EndpointDescription("Xoá tài khoản người xem cùng thư viện, lịch sử, bình luận. Không khôi phục được")]
    public static async Task<Results<NoContent, ForbidHttpResult, ValidationProblem>> DeleteAppAccount(
        ISender sender, IUser currentUser, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(currentUser.Id ?? "");
        if (user is null) return TypedResults.NoContent();

        // Tài khoản quản trị sở hữu dữ liệu của hệ thống quản lý: không cho tự xoá từ app.
        if (await userManager.IsInRoleAsync(user, Roles.Administrator)) return TypedResults.Forbid();

        await sender.Send(new DeleteAppViewerDataCommand(user.Id));
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded) return TypedResults.ValidationProblem(Users.ToValidationErrors(result));

        return TypedResults.NoContent();
    }

    [EndpointSummary("Log out app viewer")]
    [EndpointDescription("Thu hồi refresh token của người xem")]
    public static async Task<NoContent> LogoutAppViewer(IUser currentUser, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(currentUser.Id ?? "");
        if (user is not null) await Users.RevokeRefreshTokenAsync(user, userManager);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Get app bookmarks")]
    [EndpointDescription("Phim đã lưu, mới lưu trước")]
    public static async Task<Ok<List<AppMovieDto>>> GetAppBookmarks(ISender sender)
        => TypedResults.Ok(await sender.Send(new GetMyBookmarksQuery()));

    [EndpointSummary("Add app bookmark")]
    [EndpointDescription("Lưu một phim (gọi lặp lại không sinh bản trùng)")]
    public static async Task<NoContent> AddAppBookmark(ISender sender, int movieId)
    {
        await sender.Send(new SetAppBookmarkCommand(movieId, true));
        return TypedResults.NoContent();
    }

    [EndpointSummary("Remove app bookmark")]
    [EndpointDescription("Bỏ lưu một phim")]
    public static async Task<NoContent> RemoveAppBookmark(ISender sender, int movieId)
    {
        await sender.Send(new SetAppBookmarkCommand(movieId, false));
        return TypedResults.NoContent();
    }

    [EndpointSummary("Get app watch history")]
    [EndpointDescription("Lịch sử xem, mới xem trước")]
    public static async Task<Ok<List<AppHistoryDto>>> GetAppHistory(ISender sender)
        => TypedResults.Ok(await sender.Send(new GetMyHistoryQuery()));

    [EndpointSummary("Save app watch progress")]
    [EndpointDescription("Ghi tập đang xem và vị trí trong tập của một phim")]
    public static async Task<NoContent> SaveAppWatchProgress(ISender sender, int movieId, SaveAppWatchProgressRequest request)
    {
        await sender.Send(new SaveAppWatchProgressCommand
        {
            MovieId = movieId,
            EpisodeNumber = request.EpisodeNumber,
            PositionSeconds = request.PositionSeconds,
            DurationSeconds = request.DurationSeconds,
            WatchedAt = request.WatchedAt
        });
        return TypedResults.NoContent();
    }

    [EndpointSummary("Remove app watch history")]
    [EndpointDescription("Xoá một phim khỏi lịch sử xem")]
    public static async Task<NoContent> RemoveAppHistory(ISender sender, int movieId)
    {
        await sender.Send(new RemoveAppWatchHistoryCommand(movieId));
        return TypedResults.NoContent();
    }
}
