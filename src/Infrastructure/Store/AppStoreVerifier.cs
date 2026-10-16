using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Local.App.Monetization;

namespace SurveillanceCameras.Infrastructure.Store;

/// <summary>
/// Kiểm chữ ký JWS của App Store (giao dịch StoreKit 2, thông tin gia hạn, App Store Server Notification V2): header
/// <c>x5c</c> phải là chuỗi chứng chỉ do Apple Root CA - G3 cấp, chữ ký ES256 khớp chứng chỉ lá.
/// </summary>
public sealed class AppStoreVerifier : IAppStoreVerifier
{
    /// <summary>Apple Root CA - G3 (https://www.apple.com/certificateauthority/), SHA-256 63:34:3A:BF…:91:79.</summary>
    private const string AppleRootCaG3 = "MIICQzCCAcmgAwIBAgIILcX8iNLFS5UwCgYIKoZIzj0EAwMwZzEbMBkGA1UEAwwSQXBwbGUgUm9vdCBDQSAtIEczMSYwJAYDVQQLDB1BcHBsZSBDZXJ0aWZpY2F0aW9uIEF1dGhvcml0eTETMBEGA1UECgwKQXBwbGUgSW5jLjELMAkGA1UEBhMCVVMwHhcNMTQwNDMwMTgxOTA2WhcNMzkwNDMwMTgxOTA2WjBnMRswGQYDVQQDDBJBcHBsZSBSb290IENBIC0gRzMxJjAkBgNVBAsMHUFwcGxlIENlcnRpZmljYXRpb24gQXV0aG9yaXR5MRMwEQYDVQQKDApBcHBsZSBJbmMuMQswCQYDVQQGEwJVUzB2MBAGByqGSM49AgEGBSuBBAAiA2IABJjpLz1AcqTtkyJygRMc3RCV8cWjTnHcFBbZDuWmBSp3ZHtfTjjTuxxEtX/1H7YyYl3J6YRbTzBPEVoA/VhYDKX1DyxNB0cTddqXl5dvMVztK517IDvYuVTZXpmkOlEKMaNCMEAwHQYDVR0OBBYEFLuw3qFYM4iapIqZ3r6966/ayySrMA8GA1UdEwEB/wQFMAMBAf8wDgYDVR0PAQH/BAQDAgEGMAoGCCqGSM49BAMDA2gAMGUCMQCD6cHEFl4aXTQY2e3v9GwOAEZLuN+yRhHFD/3meoyhpmvOwgPUnPWTxnS4at+qIxUCMG1mihDK1A3UT82NQz60imOlM27jbdoXt2QfyFMm+YhidDkLF1vLUagM6BgD56KyKA==";

    // OID Apple đặt trong chứng chỉ ký giao dịch (lá) và chứng chỉ trung gian WWDR.
    private const string LeafOid = "1.2.840.113635.100.6.11.1";
    private const string IntermediateOid = "1.2.840.113635.100.6.2.1";

    private readonly AppStoreOptions _options;
    private readonly X509Certificate2 _root;
    private readonly TimeProvider _timeProvider;

    public AppStoreVerifier(IOptions<InAppPurchaseOptions> options, TimeProvider timeProvider)
        : this(options, timeProvider, X509CertificateLoader.LoadCertificate(Convert.FromBase64String(AppleRootCaG3)))
    {
    }

    /// <param name="root">Chứng chỉ gốc được tin (test truyền gốc tự tạo).</param>
    public AppStoreVerifier(IOptions<InAppPurchaseOptions> options, TimeProvider timeProvider, X509Certificate2 root)
    {
        _options = options.Value.AppStore;
        _timeProvider = timeProvider;
        _root = root;
    }

    public AppStoreTransaction VerifyTransaction(string? signedTransaction)
    {
        var payload = Verify(signedTransaction, "giao dịch App Store");
        var transaction = ReadTransaction(payload);
        CheckApp(transaction.BundleId, transaction.IsSandbox);
        return transaction;
    }

    public AppStoreRenewal VerifyRenewal(string? signedRenewalInfo) => ReadRenewal(Verify(signedRenewalInfo, "thông tin gia hạn App Store"));

    public AppStoreNotification VerifyNotification(string? signedPayload)
    {
        var payload = Verify(signedPayload, "thông báo App Store");
        var id = String(payload, "notificationUUID") ?? throw new StoreVerificationException("Thông báo App Store thiếu notificationUUID.");
        var type = String(payload, "notificationType") ?? "";
        var subtype = String(payload, "subtype");

        string? bundleId = null;
        var sandbox = false;
        AppStoreTransaction? transaction = null;
        AppStoreRenewal? renewal = null;
        if (payload.TryGetProperty("data", out var data))
        {
            bundleId = String(data, "bundleId");
            sandbox = IsSandbox(String(data, "environment"));
            if (String(data, "signedTransactionInfo") is { } signedTransaction)
                transaction = ReadTransaction(Verify(signedTransaction, "giao dịch App Store"));
            if (String(data, "signedRenewalInfo") is { } signedRenewal)
                renewal = ReadRenewal(Verify(signedRenewal, "thông tin gia hạn App Store"));
        }

        // Thông báo kiểm thử (TEST) không có giao dịch; vẫn phải đúng app.
        CheckApp(bundleId ?? transaction?.BundleId, sandbox);
        return new AppStoreNotification(id, type, subtype, bundleId, sandbox, transaction, renewal);
    }

    private void CheckApp(string? bundleId, bool sandbox)
    {
        if (string.IsNullOrWhiteSpace(_options.BundleId))
            throw new StoreVerificationException("Máy chủ chưa cấu hình mua trong app trên iOS (Store:AppStore:BundleId).");
        if (!string.Equals(bundleId, _options.BundleId, StringComparison.Ordinal))
            throw new StoreVerificationException("Giao dịch không thuộc app này.");
        if (sandbox && !_options.AllowSandbox)
            throw new StoreVerificationException("Máy chủ không nhận giao dịch sandbox.");
    }

    /// <summary>Kiểm chuỗi chứng chỉ và chữ ký, trả phần payload đã giải mã.</summary>
    private JsonElement Verify(string? jws, string what)
    {
        var parts = jws?.Split('.');
        if (parts is not { Length: 3 }) throw new StoreVerificationException($"Thiếu hoặc sai định dạng {what}.");

        try
        {
            using var header = JsonDocument.Parse(Base64Url(parts[0]));
            if (String(header.RootElement, "alg") != "ES256") throw new StoreVerificationException($"Thuật toán ký của {what} không phải ES256.");
            var chain = header.RootElement.GetProperty("x5c").EnumerateArray()
                .Select(x => X509CertificateLoader.LoadCertificate(Convert.FromBase64String(x.GetString()!)))
                .ToList();
            if (chain.Count < 3) throw new StoreVerificationException($"Chuỗi chứng chỉ của {what} không đủ.");

            var leaf = chain[0];
            var intermediate = chain[1];
            if (!HasExtension(leaf, LeafOid) || !HasExtension(intermediate, IntermediateOid))
                throw new StoreVerificationException($"Chứng chỉ ký {what} không phải của Apple.");

            using var x509 = new X509Chain();
            x509.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            x509.ChainPolicy.CustomTrustStore.Add(_root);
            x509.ChainPolicy.ExtraStore.Add(intermediate);
            x509.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            // Như thư viện của Apple: kiểm hiệu lực chứng chỉ tại thời điểm ký (signedDate), để giao dịch cũ vẫn kiểm được
            // sau khi chứng chỉ lá hết hạn.
            var now = _timeProvider.GetUtcNow();
            using (var unverified = JsonDocument.Parse(Base64Url(parts[1])))
            {
                var signedAt = Time(unverified.RootElement, "signedDate");
                x509.ChainPolicy.VerificationTime = (signedAt is { } at && at < now ? at : now).UtcDateTime;
            }
            if (!x509.Build(leaf) || !x509.ChainElements[^1].Certificate.RawData.AsSpan().SequenceEqual(_root.RawData))
                throw new StoreVerificationException($"Chuỗi chứng chỉ của {what} không do Apple cấp.");

            using var key = leaf.GetECDsaPublicKey() ?? throw new StoreVerificationException($"Chứng chỉ ký {what} không có khoá EC.");
            var signed = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
            if (!key.VerifyData(signed, Base64Url(parts[2]), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                throw new StoreVerificationException($"Chữ ký {what} không hợp lệ.");

            return JsonDocument.Parse(Base64Url(parts[1])).RootElement.Clone();
        }
        catch (Exception ex) when (ex is FormatException or JsonException or CryptographicException or KeyNotFoundException
                                       or InvalidOperationException)
        {
            throw new StoreVerificationException($"Không đọc được {what}.", ex);
        }
    }

    private static AppStoreTransaction ReadTransaction(JsonElement x)
        => new(
            String(x, "transactionId") ?? throw new StoreVerificationException("Giao dịch App Store thiếu transactionId."),
            String(x, "originalTransactionId") ?? String(x, "transactionId")!,
            String(x, "productId") ?? throw new StoreVerificationException("Giao dịch App Store thiếu productId."),
            String(x, "bundleId"),
            Time(x, "purchaseDate"),
            Time(x, "expiresDate"),
            Time(x, "revocationDate"),
            Guid.TryParse(String(x, "appAccountToken"), out var token) ? token : null,
            IsSandbox(String(x, "environment")));

    private static AppStoreRenewal ReadRenewal(JsonElement x)
        => new(
            x.TryGetProperty("autoRenewStatus", out var status) && status.ValueKind == JsonValueKind.Number && status.GetInt32() == 1,
            x.TryGetProperty("isInBillingRetryPeriod", out var retry) && retry.ValueKind == JsonValueKind.True,
            Time(x, "gracePeriodExpiresDate"));

    private static bool IsSandbox(string? environment) => !string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);

    private static bool HasExtension(X509Certificate2 certificate, string oid)
        => certificate.Extensions.Any(x => x.Oid?.Value == oid);

    private static string? String(JsonElement x, string name)
        => x.ValueKind == JsonValueKind.Object && x.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static DateTimeOffset? Time(JsonElement x, string name)
        => x.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? DateTimeOffset.FromUnixTimeMilliseconds(value.GetInt64()) : null;

    public static byte[] Base64Url(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }
}
