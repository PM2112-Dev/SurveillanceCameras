using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Infrastructure.Identity;

namespace SurveillanceCameras.Application.UnitTests.Security;

public class SocialTokenValidatorTests
{
    private const string WebClientId = "123-web.apps.googleusercontent.com";
    private const string BundleId = "com.aica.movie";

    private static readonly RsaSecurityKey GoogleKey = new(RSA.Create(2048)) { KeyId = "google-1" };
    private static readonly RsaSecurityKey AppleKey = new(RSA.Create(2048)) { KeyId = "apple-1" };
    private static readonly RsaSecurityKey StrangerKey = new(RSA.Create(2048)) { KeyId = "google-1" };

    private static SocialTokenValidator Validator(params string[] googleClientIds)
    {
        var options = new SocialAuthOptions();
        options.Google.ClientIds.AddRange(googleClientIds);
        options.Apple.ClientIds.Add(BundleId);

        return new SocialTokenValidator(Options.Create(options), provider =>
        {
            var configuration = new OpenIdConnectConfiguration();
            configuration.SigningKeys.Add(provider == SocialProvider.Google ? GoogleKey : AppleKey);
            return new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
        });
    }

    private static string Token(SecurityKey key, string issuer, string audience, Dictionary<string, object>? claims = null,
        DateTime? expires = null)
        => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity([new Claim("sub", "user-42")]),
            Claims = claims,
            NotBefore = (expires ?? DateTime.UtcNow.AddHours(1)).AddHours(-2),
            Expires = expires ?? DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
        });

    [Test]
    public async Task Google_ReturnsIdentity_ForATokenIssuedToOurApp()
    {
        var token = Token(GoogleKey, "https://accounts.google.com", WebClientId,
            new() { ["email"] = "an@gmail.com", ["email_verified"] = true, ["name"] = "An Nguyễn" });

        var identity = await Validator(WebClientId, "456-ios.apps.googleusercontent.com").ValidateAsync(SocialProvider.Google, token);

        identity.ShouldBe(new SocialIdentity(SocialProvider.Google, "user-42", "an@gmail.com", true, "An Nguyễn"));
    }

    [Test]
    public async Task Apple_ReadsEmailVerifiedGivenAsText_AndAllowsAMissingEmail()
    {
        var withEmail = Token(AppleKey, "https://appleid.apple.com", BundleId,
            new() { ["email"] = "x@privaterelay.appleid.com", ["email_verified"] = "true" });
        var withoutEmail = Token(AppleKey, "https://appleid.apple.com", BundleId);

        (await Validator().ValidateAsync(SocialProvider.Apple, withEmail)).EmailVerified.ShouldBeTrue();
        (await Validator().ValidateAsync(SocialProvider.Apple, withoutEmail))
            .ShouldBe(new SocialIdentity(SocialProvider.Apple, "user-42", null, false, null));
    }

    [Test]
    public async Task Rejects_TokensIssuedToAnotherApp_ByAnotherIssuer_Expired_OrSignedByAStranger()
    {
        var validator = Validator(WebClientId);
        var cases = new Dictionary<string, string>
        {
            ["app khác"] = Token(GoogleKey, "https://accounts.google.com", "app-khac.apps.googleusercontent.com"),
            ["nơi cấp khác"] = Token(GoogleKey, "https://evil.example.com", WebClientId),
            ["hết hạn"] = Token(GoogleKey, "https://accounts.google.com", WebClientId, expires: DateTime.UtcNow.AddMinutes(-10)),
            ["khoá lạ"] = Token(StrangerKey, "https://accounts.google.com", WebClientId),
            // Token Apple hợp lệ không dùng được ở cửa Google.
            ["sai nhà cung cấp"] = Token(AppleKey, "https://appleid.apple.com", BundleId),
            ["rác"] = "không-phải-jwt"
        };

        foreach (var (name, token) in cases)
        {
            await Should.ThrowAsync<SocialTokenException>(() => validator.ValidateAsync(SocialProvider.Google, token), name);
        }

        await Should.ThrowAsync<SocialTokenException>(() => validator.ValidateAsync(SocialProvider.Google, null));
    }

    [Test]
    public async Task Rejects_EveryToken_WhenTheProviderHasNoClientIdConfigured()
    {
        var token = Token(GoogleKey, "https://accounts.google.com", WebClientId);

        var ex = await Should.ThrowAsync<SocialTokenException>(() => Validator().ValidateAsync(SocialProvider.Google, token));

        ex.Message.ShouldContain("chưa cấu hình");
    }
}
