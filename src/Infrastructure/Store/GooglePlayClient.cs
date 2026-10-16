using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Local.App.Monetization;

namespace SurveillanceCameras.Infrastructure.Store;

/// <summary>
/// Google Play Developer API (<c>purchases.subscriptionsv2</c>) bằng service account. Token truy cập OAuth được ký bằng
/// khoá của service account (JWT bearer) và giữ lại tới gần hết hạn.
/// </summary>
public sealed class GooglePlayClient : IGooglePlayClient
{
    public const string HttpClientName = nameof(GooglePlayClient);
    private const string Scope = "https://www.googleapis.com/auth/androidpublisher";
    private const string Api = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications";

    private readonly GooglePlayOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;

    public GooglePlayClient(IOptions<InAppPurchaseOptions> options, IHttpClientFactory httpClientFactory, TimeProvider timeProvider)
    {
        _options = options.Value.GooglePlay;
        _httpClientFactory = httpClientFactory;
        _timeProvider = timeProvider;
    }

    public bool IsNotificationTokenValid(string? token)
        => !string.IsNullOrEmpty(_options.NotificationToken) && token is not null
           && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(_options.NotificationToken));

    public async Task<GooglePlaySubscription> GetSubscriptionAsync(string? purchaseToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(purchaseToken)) throw new StoreVerificationException("Thiếu purchase token của Google Play.");
        using var response = await SendAsync(HttpMethod.Get,
            $"{Api}/{Package()}/purchases/subscriptionsv2/tokens/{Uri.EscapeDataString(purchaseToken)}", null, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone or HttpStatusCode.BadRequest)
            throw new StoreVerificationException("Google Play không có giao dịch này.");
        await EnsureSuccessAsync(response, cancellationToken);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return Read(purchaseToken, document.RootElement);
    }

    public async Task AcknowledgeAsync(string productId, string purchaseToken, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Post,
            $"{Api}/{Package()}/purchases/subscriptions/{Uri.EscapeDataString(productId)}/tokens/{Uri.EscapeDataString(purchaseToken)}:acknowledge",
            JsonContent.Create(new { }), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>Đọc kết quả <c>SubscriptionPurchaseV2</c>: lấy kỳ có hạn xa nhất trong <c>lineItems</c>.</summary>
    public static GooglePlaySubscription Read(string purchaseToken, JsonElement x)
    {
        string? productId = null;
        DateTimeOffset? expires = null;
        var autoRenew = false;
        if (x.TryGetProperty("lineItems", out var items))
            foreach (var item in items.EnumerateArray())
            {
                var itemExpires = item.TryGetProperty("expiryTime", out var e) && DateTimeOffset.TryParse(e.GetString(), out var at) ? at : (DateTimeOffset?)null;
                if (expires is not null && itemExpires <= expires) continue;
                expires = itemExpires;
                productId = item.TryGetProperty("productId", out var p) ? p.GetString() : productId;
                autoRenew = item.TryGetProperty("autoRenewingPlan", out var plan) && plan.TryGetProperty("autoRenewEnabled", out var on)
                            && on.ValueKind == JsonValueKind.True;
            }

        string? Text(string name) => x.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        var account = x.TryGetProperty("externalAccountIdentifiers", out var ids) && ids.TryGetProperty("obfuscatedExternalAccountId", out var id)
            ? id.GetString() : null;

        return new GooglePlaySubscription(
            purchaseToken,
            Text("linkedPurchaseToken"),
            Text("subscriptionState") ?? "SUBSCRIPTION_STATE_UNSPECIFIED",
            productId,
            expires,
            autoRenew,
            Text("acknowledgementState") == "ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED",
            account,
            x.TryGetProperty("testPurchase", out _));
    }

    private string Package()
        => string.IsNullOrWhiteSpace(_options.PackageName)
            ? throw new StoreVerificationException("Máy chủ chưa cấu hình mua trong app trên Android (Store:GooglePlay:PackageName).")
            : Uri.EscapeDataString(_options.PackageName.Trim());

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await AccessTokenAsync(cancellationToken));
        try { return await client.SendAsync(request, cancellationToken); }
        catch (HttpRequestException ex) { throw new StoreVerificationException("Không kết nối được Google Play, thử lại sau.", ex); }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new StoreVerificationException($"Google Play trả lỗi {(int)response.StatusCode}: {body[..Math.Min(body.Length, 300)]}");
    }

    private async Task<string> AccessTokenAsync(CancellationToken cancellationToken)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            var now = _timeProvider.GetUtcNow();
            if (_accessToken is not null && _accessTokenExpiresAt > now.AddMinutes(2)) return _accessToken;

            var account = ServiceAccount();
            using var rsa = RSA.Create();
            rsa.ImportFromPem(account.PrivateKey);
            var assertion = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = account.ClientEmail,
                Audience = account.TokenUri,
                IssuedAt = now.UtcDateTime,
                NotBefore = now.UtcDateTime,
                Expires = now.AddMinutes(60).UtcDateTime,
                Claims = new Dictionary<string, object> { ["scope"] = Scope },
                SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa) { KeyId = account.PrivateKeyId }, SecurityAlgorithms.RsaSha256)
                    { CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false } }
            });

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsync(account.TokenUri, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = assertion
            }), cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new StoreVerificationException($"Google từ chối service account ({(int)response.StatusCode}). Kiểm tra Store:GooglePlay.");

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            _accessToken = document.RootElement.GetProperty("access_token").GetString();
            _accessTokenExpiresAt = now.AddSeconds(document.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600);
            return _accessToken!;
        }
        catch (HttpRequestException ex)
        {
            throw new StoreVerificationException("Không lấy được quyền truy cập Google Play, thử lại sau.", ex);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private sealed record Account(string ClientEmail, string PrivateKey, string? PrivateKeyId, string TokenUri);

    private Account ServiceAccount()
    {
        var json = !string.IsNullOrWhiteSpace(_options.ServiceAccountJson) ? _options.ServiceAccountJson
            : !string.IsNullOrWhiteSpace(_options.ServiceAccountFile) && File.Exists(_options.ServiceAccountFile) ? File.ReadAllText(_options.ServiceAccountFile)
            : throw new StoreVerificationException("Máy chủ chưa cấu hình service account Google Play (Store:GooglePlay:ServiceAccountJson).");
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            return new Account(root.GetProperty("client_email").GetString()!, root.GetProperty("private_key").GetString()!,
                root.TryGetProperty("private_key_id", out var id) ? id.GetString() : null,
                root.TryGetProperty("token_uri", out var uri) ? uri.GetString()! : "https://oauth2.googleapis.com/token");
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new StoreVerificationException("File service account Google Play không hợp lệ.", ex);
        }
    }
}
