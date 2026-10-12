using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace SurveillanceCameras.Infrastructure.Identity;

public enum SocialProvider
{
    Google,
    Apple
}

/// <summary>Cấu hình đăng nhập bằng Google/Apple cho app (mục <c>SocialAuth</c> trong appsettings).</summary>
public class SocialAuthOptions
{
    public const string SectionName = "SocialAuth";

    public SocialProviderOptions Google { get; set; } = new();

    public SocialProviderOptions Apple { get; set; } = new();

    /// <summary>
    /// Scheme của deep link mà máy chủ chuyển về app Android sau khi đăng nhập Apple trên web
    /// (phải trùng scheme khai báo trong AndroidManifest của app).
    /// </summary>
    public string AndroidRedirectScheme { get; set; } = "luma-auth";

    public SocialProviderOptions For(SocialProvider provider) => provider == SocialProvider.Google ? Google : Apple;
}

public class SocialProviderOptions
{
    /// <summary>
    /// Các client ID được chấp nhận (claim <c>aud</c> của ID token). Google: OAuth client ID loại Web (app Android dùng làm
    /// serverClientId) và loại iOS. Apple: bundle ID của app iOS và Services ID (đăng nhập Apple trên Android).
    /// Bỏ trống thì nhà cung cấp đó tắt.
    /// </summary>
    public List<string> ClientIds { get; set; } = [];
}

/// <param name="Subject">ID bất biến của người dùng ở nhà cung cấp (claim <c>sub</c>).</param>
/// <param name="EmailVerified">Nhà cung cấp xác nhận người dùng sở hữu <paramref name="Email"/>.</param>
public sealed record SocialIdentity(SocialProvider Provider, string Subject, string? Email, bool EmailVerified, string? Name);

/// <summary>ID token không hợp lệ (sai chữ ký, sai app, hết hạn) hoặc nhà cung cấp chưa được cấu hình.</summary>
public class SocialTokenException : Exception
{
    public SocialTokenException(string message, Exception? innerException = null) : base(message, innerException) { }
}

public interface ISocialTokenValidator
{
    /// <summary>Kiểm tra ID token do Google/Apple cấp cho app (chữ ký, nơi cấp, app nhận, hạn dùng) và đọc danh tính.</summary>
    /// <exception cref="SocialTokenException">Token không hợp lệ hoặc nhà cung cấp chưa cấu hình client ID.</exception>
    Task<SocialIdentity> ValidateAsync(SocialProvider provider, string? idToken, CancellationToken cancellationToken = default);
}

public sealed class SocialTokenValidator : ISocialTokenValidator
{
    private static readonly Dictionary<SocialProvider, (string Metadata, string[] Issuers)> Providers = new()
    {
        [SocialProvider.Google] = ("https://accounts.google.com/.well-known/openid-configuration",
            ["https://accounts.google.com", "accounts.google.com"]),
        [SocialProvider.Apple] = ("https://appleid.apple.com/.well-known/openid-configuration", ["https://appleid.apple.com"])
    };

    private readonly SocialAuthOptions _options;
    private readonly Dictionary<SocialProvider, BaseConfigurationManager> _configurations;
    private readonly JsonWebTokenHandler _handler = new() { MapInboundClaims = false };

    public SocialTokenValidator(IOptions<SocialAuthOptions> options, IHttpClientFactory httpClientFactory)
        : this(options, provider => new ConfigurationManager<OpenIdConnectConfiguration>(
            Providers[provider].Metadata, new OpenIdConnectConfigurationRetriever(),
            // Khoá ký của Google/Apple được tải một lần rồi tự làm mới định kỳ (và khi gặp khoá lạ).
            new HttpDocumentRetriever(httpClientFactory.CreateClient(nameof(SocialTokenValidator)))))
    {
    }

    /// <param name="configurationFor">Nguồn khoá ký của từng nhà cung cấp (test truyền khoá cố định).</param>
    public SocialTokenValidator(IOptions<SocialAuthOptions> options, Func<SocialProvider, BaseConfigurationManager> configurationFor)
    {
        _options = options.Value;
        _configurations = Providers.Keys.ToDictionary(x => x, configurationFor);
    }

    public async Task<SocialIdentity> ValidateAsync(SocialProvider provider, string? idToken,
        CancellationToken cancellationToken = default)
    {
        var audiences = _options.For(provider).ClientIds.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (audiences.Count == 0)
            throw new SocialTokenException($"Máy chủ chưa cấu hình đăng nhập bằng {provider} (SocialAuth:{provider}:ClientIds).");
        if (string.IsNullOrWhiteSpace(idToken))
            throw new SocialTokenException($"Thiếu ID token của {provider}.");

        TokenValidationResult result;
        try
        {
            result = await _handler.ValidateTokenAsync(idToken, new TokenValidationParameters
            {
                ConfigurationManager = _configurations[provider],
                ValidIssuers = Providers[provider].Issuers,
                ValidAudiences = audiences,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            });
        }
        // Không tải được khoá ký của nhà cung cấp (mất mạng…).
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or IOException)
        {
            throw new SocialTokenException($"Không kiểm tra được đăng nhập {provider} lúc này, thử lại sau.", ex);
        }

        if (!result.IsValid)
            throw new SocialTokenException($"Phiên đăng nhập {provider} không hợp lệ hoặc đã hết hạn. Thử đăng nhập lại.", result.Exception);

        var subject = Claim(result, "sub");
        if (string.IsNullOrEmpty(subject))
            throw new SocialTokenException($"ID token của {provider} thiếu mã người dùng.");

        // Apple trả email_verified dạng chuỗi "true", Google dạng boolean.
        var verified = result.Claims.TryGetValue("email_verified", out var raw)
                       && (raw is true || string.Equals(raw?.ToString(), "true", StringComparison.OrdinalIgnoreCase));

        return new SocialIdentity(provider, subject, Claim(result, "email"), verified, Claim(result, "name"));
    }

    private static string? Claim(TokenValidationResult result, string name)
        => result.Claims.TryGetValue(name, out var value) && value?.ToString() is { Length: > 0 } text ? text : null;
}
