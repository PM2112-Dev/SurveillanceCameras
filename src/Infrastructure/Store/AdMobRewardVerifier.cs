using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Local.App.Monetization;

namespace SurveillanceCameras.Infrastructure.Store;

/// <summary>
/// Kiểm callback server-side verification của quảng cáo có thưởng AdMob: chữ ký ECDSA (SHA-256) trên phần query trước
/// <c>&amp;signature=</c>, theo khoá công khai Google công bố (tải lại khi gặp <c>key_id</c> lạ, nếu không thì mỗi ngày).
/// </summary>
public sealed class AdMobRewardVerifier : IAdMobRewardVerifier
{
    public const string HttpClientName = nameof(AdMobRewardVerifier);
    private const string KeysUrl = "https://www.gstatic.com/admob/reward/verifier-keys.json";
    private static readonly TimeSpan KeysLifetime = TimeSpan.FromHours(24);

    private readonly Func<CancellationToken, Task<string>> _downloadKeys;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Dictionary<long, ECDsa> _keys = [];
    private DateTimeOffset _keysLoadedAt = DateTimeOffset.MinValue;

    public AdMobRewardVerifier(IHttpClientFactory httpClientFactory, TimeProvider timeProvider)
        : this(ct => httpClientFactory.CreateClient(HttpClientName).GetStringAsync(KeysUrl, ct), timeProvider)
    {
    }

    /// <param name="downloadKeys">Trả nội dung <c>verifier-keys.json</c> (test truyền khoá tự tạo).</param>
    public AdMobRewardVerifier(Func<CancellationToken, Task<string>> downloadKeys, TimeProvider timeProvider)
    {
        _downloadKeys = downloadKeys;
        _timeProvider = timeProvider;
    }

    public async Task<AdMobReward> VerifyAsync(string? queryString, CancellationToken cancellationToken)
    {
        var query = queryString?.TrimStart('?') ?? "";
        var signatureAt = query.IndexOf("&signature=", StringComparison.Ordinal);
        if (signatureAt <= 0) throw new StoreVerificationException("Callback AdMob thiếu chữ ký.");

        var message = Encoding.UTF8.GetBytes(query[..signatureAt]);
        var values = HttpUtility.ParseQueryString(query);
        if (!long.TryParse(values["key_id"], out var keyId)) throw new StoreVerificationException("Callback AdMob thiếu key_id.");

        byte[] signature;
        try { signature = AppStoreVerifier.Base64Url(values["signature"] ?? ""); }
        catch (FormatException ex) { throw new StoreVerificationException("Chữ ký callback AdMob sai định dạng.", ex); }

        var key = await KeyAsync(keyId, cancellationToken) ?? throw new StoreVerificationException($"Khoá AdMob {keyId} không tồn tại.");
        bool valid;
        try { valid = key.VerifyData(message, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence); }
        catch (CryptographicException) { valid = false; }
        if (!valid) throw new StoreVerificationException("Chữ ký callback AdMob không hợp lệ.");

        var transactionId = values["transaction_id"];
        if (string.IsNullOrWhiteSpace(transactionId)) throw new StoreVerificationException("Callback AdMob thiếu transaction_id.");
        var timestamp = long.TryParse(values["timestamp"], out var ms) ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : _timeProvider.GetUtcNow();

        return new AdMobReward(transactionId, values["user_id"], values["custom_data"], values["ad_unit"], timestamp);
    }

    private async Task<ECDsa?> KeyAsync(long keyId, CancellationToken cancellationToken)
    {
        var fresh = _timeProvider.GetUtcNow() - _keysLoadedAt < KeysLifetime;
        if (fresh && _keys.TryGetValue(keyId, out var cached)) return cached;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_timeProvider.GetUtcNow() - _keysLoadedAt >= KeysLifetime || !_keys.ContainsKey(keyId))
            {
                string json;
                try { json = await _downloadKeys(cancellationToken); }
                catch (HttpRequestException ex) { throw new StoreVerificationException("Không tải được khoá xác minh của AdMob.", ex); }

                var keys = new Dictionary<long, ECDsa>();
                using var document = JsonDocument.Parse(json);
                foreach (var item in document.RootElement.GetProperty("keys").EnumerateArray())
                {
                    var key = ECDsa.Create();
                    key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(item.GetProperty("base64").GetString()!), out _);
                    keys[item.GetProperty("keyId").GetInt64()] = key;
                }

                _keys = keys;
                _keysLoadedAt = _timeProvider.GetUtcNow();
            }

            return _keys.GetValueOrDefault(keyId);
        }
        finally
        {
            _lock.Release();
        }
    }
}
