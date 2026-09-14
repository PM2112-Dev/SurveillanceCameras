using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Infrastructure.Identity;

namespace SurveillanceCameras.Web.Infrastructure;

/// <summary>
/// DEV-ONLY authentication scheme that auto-authenticates every request as the seeded
/// Administrator account (see <c>ApplicationDbContextInitialiser.TrySeedAsync</c>), so testing
/// via Scalar does not require pasting a real JWT.
///
/// This handler is only ever wired up when <c>IHostEnvironment.IsDevelopment()</c> is true
/// (see <see cref="Microsoft.Extensions.DependencyInjection.DependencyInjection.AddWebServices"/>) —
/// it is never registered in Staging/Production, so it can never bypass real auth outside local dev.
/// Requests that DO send a real Bearer token still authenticate via JWT as normal (see the
/// "DevOrJwt" policy scheme), so role-specific testing with a real token still works.
/// </summary>
public class DevAutoAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DevAuto";
    private const string DevAdminUserName = "administrator@localhost";

    private readonly UserManager<ApplicationUser> _userManager;

    public DevAutoAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        UserManager<ApplicationUser> userManager)
        : base(options, logger, encoder)
    {
        _userManager = userManager;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var user = await _userManager.FindByNameAsync(DevAdminUserName);
        if (user is null)
        {
            // Seed hasn't run yet (or was changed) — fail closed, let the request continue
            // unauthenticated instead of crashing, so normal 401 behaviour still applies.
            return AuthenticateResult.NoResult();
        }

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? DevAdminUserName),
            new(ClaimTypes.Email, user.Email ?? DevAdminUserName)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
