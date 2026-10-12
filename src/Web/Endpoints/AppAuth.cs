using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Domain.Constants;
using SurveillanceCameras.Infrastructure.Data;
using SurveillanceCameras.Infrastructure.Identity;

namespace SurveillanceCameras.Web.Endpoints;

/// <summary>
/// Đăng ký/đăng nhập của người xem trên app — bằng email + mật khẩu, hoặc bằng tài khoản Google/Apple. Dùng chung tài khoản
/// và JWT với <see cref="Users"/>; tài khoản mới luôn có quyền <see cref="Roles.User"/>.
/// </summary>
public class AppAuth : IEndpointGroup
{
    public const int MaxDisplayNameLength = 50;

    public static string? RoutePrefix => "/api/app/auth";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(RegisterAppViewer, "register");
        groupBuilder.MapPost(LoginAppViewer, "login");
        groupBuilder.MapPost(RefreshAppViewer, "refresh");
        groupBuilder.MapPost(LoginAppViewerWithGoogle, "google");
        groupBuilder.MapPost(LoginAppViewerWithApple, "apple");
        groupBuilder.MapPost(AppleWebSignInCallback, "apple/callback").ExcludeFromDescription();
    }

    /// <param name="IdToken">ID token do Google/Apple cấp cho app sau khi người dùng đăng nhập trên máy.</param>
    /// <param name="DisplayName">Tên người dùng do app gửi kèm — Apple chỉ đưa tên cho app ở lần đăng nhập đầu, không có trong token.</param>
    public sealed record AppSocialLoginRequest(string? IdToken, string? DisplayName);

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

    [EndpointSummary("Log in app viewer with Google")]
    [EndpointDescription("Đăng nhập bằng ID token của Google; lần đầu thì tạo tài khoản người xem (quyền User)")]
    public static Task<Results<Ok<AppAuthResponse>, ValidationProblem>> LoginAppViewerWithGoogle(
        ISocialTokenValidator validator, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext, IConfiguration configuration, TimeProvider timeProvider,
        AppSocialLoginRequest request, CancellationToken cancellationToken)
        => SocialSignInAsync(SocialProvider.Google, request, validator, userManager, roleManager, dbContext, configuration,
            timeProvider, cancellationToken);

    [EndpointSummary("Log in app viewer with Apple")]
    [EndpointDescription("Đăng nhập bằng identity token của Apple; lần đầu thì tạo tài khoản người xem (quyền User)")]
    public static Task<Results<Ok<AppAuthResponse>, ValidationProblem>> LoginAppViewerWithApple(
        ISocialTokenValidator validator, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext, IConfiguration configuration, TimeProvider timeProvider,
        AppSocialLoginRequest request, CancellationToken cancellationToken)
        => SocialSignInAsync(SocialProvider.Apple, request, validator, userManager, roleManager, dbContext, configuration,
            timeProvider, cancellationToken);

    /// <summary>
    /// Nơi Apple gửi kết quả đăng nhập trên web về (app Android không có SDK Apple nên đăng nhập qua trình duyệt; Apple chỉ
    /// trả về một địa chỉ https bằng form POST). Máy chủ không xử lý gì ở đây, chỉ chuyển token về app qua deep link; app
    /// kiểm tra <c>state</c> rồi gọi <c>auth/apple</c> như iOS — token vẫn được kiểm tra chữ ký ở đó.
    /// </summary>
    public static async Task<IResult> AppleWebSignInCallback(HttpRequest request, IOptions<SocialAuthOptions> options)
    {
        if (!request.HasFormContentType) return TypedResults.BadRequest();

        var form = await request.ReadFormAsync();
        var query = new Dictionary<string, string?>
        {
            ["state"] = form["state"],
            ["id_token"] = form["id_token"],
            ["error"] = form["error"],
            ["name"] = AppleUserName(form["user"])
        };

        return TypedResults.Redirect(QueryHelpers.AddQueryString($"{options.Value.AndroidRedirectScheme}://apple",
            query.Where(x => !string.IsNullOrEmpty(x.Value))));
    }

    /// <summary>Tên trong trường <c>user</c> (JSON) mà Apple gửi kèm ở lần đăng nhập đầu tiên.</summary>
    private static string? AppleUserName(string? userJson)
    {
        if (string.IsNullOrWhiteSpace(userJson)) return null;

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(userJson);
            if (!document.RootElement.TryGetProperty("name", out var name)) return null;

            var parts = new[] { "firstName", "lastName" }
                .Select(x => name.TryGetProperty(x, out var part) ? part.GetString() : null)
                .Where(x => !string.IsNullOrWhiteSpace(x));
            return CleanDisplayName(string.Join(' ', parts));
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static async Task<Results<Ok<AppAuthResponse>, ValidationProblem>> SocialSignInAsync(SocialProvider provider,
        AppSocialLoginRequest request, ISocialTokenValidator validator, UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager, ApplicationDbContext dbContext, IConfiguration configuration,
        TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        SocialIdentity identity;
        try
        {
            identity = await validator.ValidateAsync(provider, request.IdToken, cancellationToken);
        }
        catch (SocialTokenException ex)
        {
            return Problem(ex.Message);
        }

        var loginProvider = provider.ToString();
        var user = await userManager.FindByLoginAsync(loginProvider, identity.Subject);
        if (user is null)
        {
            // Chỉ tin email khi nhà cung cấp xác nhận người dùng sở hữu nó.
            var email = identity.EmailVerified ? identity.Email : null;
            user = email is null ? null : await userManager.FindByEmailAsync(email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email ?? $"{loginProvider.ToLowerInvariant()}-{identity.Subject}",
                    Email = email,
                    EmailConfirmed = email is not null,
                    DisplayName = CleanDisplayName(request.DisplayName) ?? CleanDisplayName(identity.Name) ?? email?.Split('@')[0]
                };

                var created = await userManager.CreateAsync(user);
                if (!created.Succeeded) return TypedResults.ValidationProblem(Users.ToValidationErrors(created));

                if (!await roleManager.RoleExistsAsync(Roles.User)) await roleManager.CreateAsync(new IdentityRole(Roles.User));
                await userManager.AddToRoleAsync(user, Roles.User);
            }
            // Tài khoản đăng ký bằng mật khẩu chưa từng xác nhận email: có thể do người khác tạo trước bằng email này, nên
            // không tự gộp (gộp là trao tài khoản — và mật khẩu người kia đặt — cho chủ email).
            else if (!user.EmailConfirmed)
            {
                return Problem("Email này đã được đăng ký bằng mật khẩu. Hãy đăng nhập bằng email và mật khẩu.");
            }
            // Tài khoản quản trị không nhận thêm cách đăng nhập từ app.
            else if (await userManager.IsInRoleAsync(user, Roles.Administrator))
            {
                return Problem("Tài khoản này không đăng nhập được bằng " + loginProvider + ".");
            }

            var linked = await userManager.AddLoginAsync(user, new UserLoginInfo(loginProvider, identity.Subject, loginProvider));
            if (!linked.Succeeded) return TypedResults.ValidationProblem(Users.ToValidationErrors(linked));
        }

        return TypedResults.Ok(await SignInAsync(user, userManager, dbContext, configuration, timeProvider));

        static ValidationProblem Problem(string message)
            => TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["IdToken"] = [message] });
    }

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
