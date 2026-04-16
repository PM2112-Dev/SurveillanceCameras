using SurveillanceCameras.Infrastructure.Identity;
using SurveillanceCameras.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SurveillanceCameras.Web.Endpoints;

public class Users : IEndpointGroup
{
    private const string RefreshTokenProvider = "SurveillanceCameras.Jwt";
    private const string RefreshTokenName = "RefreshTokenHash";
    private const string RefreshTokenExpiryName = "RefreshTokenExpiryUtc";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Register, "register");
        groupBuilder.MapPost(Login, "login");
        groupBuilder.MapPost(Refresh, "refresh");
        groupBuilder.MapGet(GetInfo, "manage/info").RequireAuthorization();
        groupBuilder.MapPost(Logout, "logout").RequireAuthorization();
    }

    [EndpointSummary("Register")]
    [EndpointDescription("Creates a new user account with email and password.")]
    public static async Task<Results<Ok, ValidationProblem>> Register(
        UserManager<ApplicationUser> userManager,
        RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (result.Succeeded)
        {
            return TypedResults.Ok();
        }

        return TypedResults.ValidationProblem(ToValidationErrors(result));
    }

    [EndpointSummary("Log in")]
    [EndpointDescription("Authenticates a user and returns a JWT access token.")]
    public static async Task<Results<Ok<AccessTokenResponse>, UnauthorizedHttpResult>> Login(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var isValidPassword = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
        {
            return TypedResults.Unauthorized();
        }

        var tokenResponse = await CreateTokenResponseAsync(user, userManager, configuration);
        return TypedResults.Ok(tokenResponse);
    }

    [EndpointSummary("Refresh token")]
    [EndpointDescription("Returns a new JWT access token and rotates the refresh token.")]
    public static async Task<Results<Ok<AccessTokenResponse>, UnauthorizedHttpResult>> Refresh(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return TypedResults.Unauthorized();
        }

        var refreshTokenHash = HashRefreshToken(request.RefreshToken);
        var storedToken = await dbContext.Set<IdentityUserToken<string>>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.LoginProvider == RefreshTokenProvider
                && t.Name == RefreshTokenName
                && t.Value == refreshTokenHash);

        if (storedToken is null)
        {
            return TypedResults.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(storedToken.UserId);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var refreshExpiryValue = await userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName);
        if (!DateTimeOffset.TryParse(refreshExpiryValue, out var refreshExpiryUtc) || refreshExpiryUtc <= DateTimeOffset.UtcNow)
        {
            await RevokeRefreshTokenAsync(user, userManager);
            return TypedResults.Unauthorized();
        }

        var tokenResponse = await CreateTokenResponseAsync(user, userManager, configuration);
        return TypedResults.Ok(tokenResponse);
    }

    [EndpointSummary("Get account info")]
    [EndpointDescription("Returns basic information for the current authenticated user.")]
    public static async Task<Results<Ok<InfoResponse>, UnauthorizedHttpResult>> GetInfo(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return TypedResults.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(new InfoResponse(user.Email ?? string.Empty, user.EmailConfirmed));
    }

    [EndpointSummary("Log out")]
    [EndpointDescription("Revokes the current user's refresh token and clears local auth state on the client.")]
    public static async Task<Ok> Logout(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        [FromBody] object? empty)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user is not null)
            {
                await RevokeRefreshTokenAsync(user, userManager);
            }
        }

        return TypedResults.Ok();
    }

    private static async Task<AccessTokenResponse> CreateTokenResponseAsync(
        ApplicationUser user,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"]!;
        var audience = jwtSection["Audience"]!;
        var signingKey = jwtSection["SigningKey"]!;
        var accessTokenExpirationMinutes = jwtSection.GetValue<int?>("AccessTokenExpirationMinutes") ?? 60;
        var refreshTokenExpirationDays = jwtSection.GetValue<int?>("RefreshTokenExpirationDays") ?? 7;

        var userRoles = await userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expiresAt = DateTime.UtcNow.AddMinutes(accessTokenExpirationMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiryUtc = DateTimeOffset.UtcNow.AddDays(refreshTokenExpirationDays);

        await userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, HashRefreshToken(refreshToken));
        await userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName, refreshTokenExpiryUtc.ToString("O"));

        return new AccessTokenResponse(
            "Bearer",
            new JwtSecurityTokenHandler().WriteToken(token),
            (int)(expiresAt - DateTime.UtcNow).TotalSeconds,
            refreshToken);
    }

    private static async Task RevokeRefreshTokenAsync(ApplicationUser user, UserManager<ApplicationUser> userManager)
    {
        await userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        await userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName);
    }

    private static string GenerateRefreshToken()
    {
        Span<byte> randomBytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }

    private static Dictionary<string, string[]> ToValidationErrors(IdentityResult result)
    {
        return result.Errors
            .GroupBy(x => x.Code)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Description).ToArray());
    }

    public sealed record RegisterRequest(string Email, string Password);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record RefreshRequest(string RefreshToken);
    public sealed record AccessTokenResponse(string TokenType, string AccessToken, int ExpiresIn, string RefreshToken);
    public sealed record InfoResponse(string Email, bool IsEmailConfirmed);
}
