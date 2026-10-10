using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SurveillanceCameras.Domain.Constants;
using SurveillanceCameras.Infrastructure.Data;
using SurveillanceCameras.Infrastructure.Identity;

namespace SurveillanceCameras.Web.Endpoints;

/// <summary>Đăng ký/đăng nhập của người xem trên app. Dùng chung tài khoản và JWT với <see cref="Users"/>.</summary>
public class AppAuth : IEndpointGroup
{
    public const int MaxDisplayNameLength = 50;

    public static string? RoutePrefix => "/api/app/auth";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(RegisterAppViewer, "register");
        groupBuilder.MapPost(LoginAppViewer, "login");
        groupBuilder.MapPost(RefreshAppViewer, "refresh");
    }

    public sealed record AppRegisterRequest(string? Email, string? Password, string? DisplayName);

    public sealed record AppLoginRequest(string? Email, string? Password);

    public sealed record AppRefreshRequest(string? RefreshToken);

    public sealed record AppProfileDto(string Email, string DisplayName, bool IsVip, string? VipPlan, DateTimeOffset? VipExpiresAt);

    public sealed record AppAuthResponse(string AccessToken, int ExpiresIn, string RefreshToken, AppProfileDto Profile);

    [EndpointSummary("Register app viewer")]
    [EndpointDescription("Tạo tài khoản người xem bằng email + mật khẩu và đăng nhập luôn")]
    public static async Task<Results<Ok<AppAuthResponse>, ValidationProblem>> RegisterAppViewer(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        TimeProvider timeProvider,
        AppRegisterRequest request)
    {
        var email = request.Email?.Trim() ?? "";
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = CleanDisplayName(request.DisplayName) ?? email.Split('@')[0]
        };

        var result = await userManager.CreateAsync(user, request.Password ?? "");
        if (!result.Succeeded) return TypedResults.ValidationProblem(Users.ToValidationErrors(result));

        if (!await roleManager.RoleExistsAsync(Roles.User)) await roleManager.CreateAsync(new IdentityRole(Roles.User));
        await userManager.AddToRoleAsync(user, Roles.User);

        return TypedResults.Ok(await SignInAsync(user, userManager, dbContext, configuration, timeProvider));
    }

    [EndpointSummary("Log in app viewer")]
    [EndpointDescription("Đăng nhập người xem, trả về JWT kèm hồ sơ")]
    public static async Task<Results<Ok<AppAuthResponse>, UnauthorizedHttpResult>> LoginAppViewer(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        TimeProvider timeProvider,
        AppLoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email?.Trim() ?? "");
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password ?? ""))
            return TypedResults.Unauthorized();

        return TypedResults.Ok(await SignInAsync(user, userManager, dbContext, configuration, timeProvider));
    }

    [EndpointSummary("Refresh app viewer token")]
    [EndpointDescription("Đổi refresh token lấy JWT mới (refresh token cũ hết hiệu lực)")]
    public static async Task<Results<Ok<Users.AccessTokenResponse>, UnauthorizedHttpResult>> RefreshAppViewer(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        AppRefreshRequest request)
        => await Users.Refresh(dbContext, userManager, configuration, new Users.RefreshRequest(request.RefreshToken ?? ""));

    internal static async Task<AppAuthResponse> SignInAsync(ApplicationUser user, UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext, IConfiguration configuration, TimeProvider timeProvider)
    {
        var tokens = await Users.CreateTokenResponseAsync(user, userManager, configuration);
        return new AppAuthResponse(tokens.AccessToken, tokens.ExpiresIn, tokens.RefreshToken,
            await ProfileAsync(user, dbContext, timeProvider));
    }

    internal static async Task<AppProfileDto> ProfileAsync(ApplicationUser user, ApplicationDbContext dbContext,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        var subscription = await dbContext.AppSubscriptions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.ExpiresAt > now);

        return new AppProfileDto(user.Email ?? "", DisplayNameOf(user) ?? "", subscription is not null,
            subscription?.Plan, subscription?.ExpiresAt);
    }

    internal static string? DisplayNameOf(ApplicationUser? user)
        => user is null ? null
            : !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName
            : user.Email?.Split('@')[0];

    /// <summary>Tên hiển thị gọn một dòng, tối đa <see cref="MaxDisplayNameLength"/> ký tự; rỗng thì <c>null</c>.</summary>
    internal static string? CleanDisplayName(string? name)
    {
        var clean = string.Join(' ', (name ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (clean.Length == 0) return null;
        return clean.Length <= MaxDisplayNameLength ? clean : clean[..MaxDisplayNameLength];
    }
}
