using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Infrastructure.Store;

namespace SurveillanceCameras.Application.UnitTests.Monetization;

/// <summary>Kiểm chữ ký App Store (JWS + chuỗi chứng chỉ) và AdMob SSV bằng khoá tự tạo theo đúng cấu trúc của Apple/Google.</summary>
public class StoreVerifierTests
{
    private const string BundleId = "com.aica.movie";

    /// <summary>Chuỗi gốc → trung gian (OID WWDR) → lá (OID ký giao dịch) giống của Apple.</summary>
    private sealed class FakeApple : IDisposable
    {
        public readonly X509Certificate2 Root, Intermediate, Leaf;
        private readonly ECDsa _leafKey;

        public FakeApple(string name = "Fake Apple Root")
        {
            using var rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var rootRequest = new CertificateRequest($"CN={name}", rootKey, HashAlgorithmName.SHA256);
            rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            Root = rootRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddYears(-1), DateTimeOffset.UtcNow.AddYears(5));

            using var intermediateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var intermediateRequest = new CertificateRequest("CN=Fake WWDR", intermediateKey, HashAlgorithmName.SHA256);
            intermediateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            intermediateRequest.CertificateExtensions.Add(new X509Extension("1.2.840.113635.100.6.2.1", [5, 0], false));
            Intermediate = intermediateRequest.Create(Root, DateTimeOffset.UtcNow.AddMonths(-6), DateTimeOffset.UtcNow.AddYears(2), [1])
                .CopyWithPrivateKey(intermediateKey);

            _leafKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var leafRequest = new CertificateRequest("CN=Fake StoreKit", _leafKey, HashAlgorithmName.SHA256);
            leafRequest.CertificateExtensions.Add(new X509Extension("1.2.840.113635.100.6.11.1", [5, 0], false));
            Leaf = leafRequest.Create(Intermediate, DateTimeOffset.UtcNow.AddMonths(-1), DateTimeOffset.UtcNow.AddYears(1), [2]);
        }

        public string Sign(object payload, ECDsa? signWith = null)
        {
            var header = new { alg = "ES256", x5c = new[] { Leaf, Intermediate, Root }.Select(x => Convert.ToBase64String(x.RawData)) };
            var signingInput = Base64Url(JsonSerializer.SerializeToUtf8Bytes(header)) + "." + Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload));
            var signature = (signWith ?? _leafKey).SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256);
            return signingInput + "." + Base64Url(signature);
        }

        public void Dispose() => _leafKey.Dispose();
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static AppStoreVerifier Verifier(X509Certificate2 root, bool allowSandbox = true)
        => new(Options.Create(new InAppPurchaseOptions { AppStore = new AppStoreOptions { BundleId = BundleId, AllowSandbox = allowSandbox } }),
            TimeProvider.System, root);

    private static object Transaction(string bundleId = BundleId, string environment = "Sandbox", Guid? account = null) => new
    {
        transactionId = "2000000111", originalTransactionId = "2000000100", productId = "luma.vip.month", bundleId,
        purchaseDate = 1_760_000_000_000L, expiresDate = 1_762_592_000_000L, appAccountToken = (account ?? Guid.Empty).ToString(),
        environment, signedDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    };

    [Test]
    public void AppStoreTransaction_SignedThroughTheTrustedRoot_IsReadBack()
    {
        using var apple = new FakeApple();
        var account = Guid.NewGuid();

        var transaction = Verifier(apple.Root).VerifyTransaction(apple.Sign(Transaction(account: account)));

        transaction.OriginalTransactionId.ShouldBe("2000000100");
        transaction.ProductId.ShouldBe("luma.vip.month");
        transaction.ExpiresAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1_762_592_000_000L));
        transaction.AppAccountToken.ShouldBe(account);
        transaction.IsSandbox.ShouldBeTrue();
    }

    [Test]
    public void AppStoreTransaction_IsRejectedWhenForged_ForAnotherApp_FromAnotherRoot_OrSandboxIsOff()
    {
        using var apple = new FakeApple();
        using var impostor = new FakeApple("Impostor Root");
        using var otherKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        Should.Throw<StoreVerificationException>(() => Verifier(apple.Root).VerifyTransaction(apple.Sign(Transaction(), otherKey)));
        Should.Throw<StoreVerificationException>(() => Verifier(apple.Root).VerifyTransaction(apple.Sign(Transaction("com.other.app"))));
        Should.Throw<StoreVerificationException>(() => Verifier(apple.Root).VerifyTransaction(impostor.Sign(Transaction())));
        Should.Throw<StoreVerificationException>(() => Verifier(apple.Root, allowSandbox: false).VerifyTransaction(apple.Sign(Transaction())));
        Should.Throw<StoreVerificationException>(() => Verifier(apple.Root).VerifyTransaction("not-a-jws"));

        // Đổi payload sau khi ký.
        var parts = apple.Sign(Transaction()).Split('.');
        var forged = parts[0] + "." + Base64Url(JsonSerializer.SerializeToUtf8Bytes(Transaction(environment: "Production"))) + "." + parts[2];
        Should.Throw<StoreVerificationException>(() => Verifier(apple.Root).VerifyTransaction(forged));
    }

    [Test]
    public void AppStoreNotification_CarriesTheSignedTransactionAndRenewalInfo()
    {
        using var apple = new FakeApple();
        var payload = new
        {
            notificationType = "DID_FAIL_TO_RENEW", subtype = "GRACE_PERIOD", notificationUUID = "uuid-1",
            data = new
            {
                bundleId = BundleId, environment = "Sandbox",
                signedTransactionInfo = apple.Sign(Transaction()),
                signedRenewalInfo = apple.Sign(new { autoRenewStatus = 1, isInBillingRetryPeriod = true, gracePeriodExpiresDate = 1_763_000_000_000L })
            }
        };

        var notification = Verifier(apple.Root).VerifyNotification(apple.Sign(payload));

        notification.NotificationId.ShouldBe("uuid-1");
        notification.Type.ShouldBe("DID_FAIL_TO_RENEW");
        notification.Subtype.ShouldBe("GRACE_PERIOD");
        notification.Transaction!.OriginalTransactionId.ShouldBe("2000000100");
        notification.Renewal.ShouldBe(new(true, true, DateTimeOffset.FromUnixTimeMilliseconds(1_763_000_000_000L)));
    }

    [Test]
    public void TheRealAppleRootIsEmbedded()
    {
        using var apple = new FakeApple();
        // Không có gốc tự tạo: chỉ chuỗi của Apple thật mới qua.
        var verifier = new AppStoreVerifier(Options.Create(new InAppPurchaseOptions { AppStore = new AppStoreOptions { BundleId = BundleId } }),
            TimeProvider.System);
        Should.Throw<StoreVerificationException>(() => verifier.VerifyTransaction(apple.Sign(Transaction())));
    }

    private static (AdMobRewardVerifier Verifier, ECDsa Key) AdMob(long keyId = 3335741209)
    {
        var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var json = JsonSerializer.Serialize(new { keys = new[] { new { keyId, pem = "", base64 = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()) } } });
        return (new AdMobRewardVerifier(_ => Task.FromResult(json), TimeProvider.System), key);
    }

    private static string AdMobQuery(ECDsa key, string content, long keyId = 3335741209)
    {
        var signature = key.SignData(Encoding.UTF8.GetBytes(content), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        return $"?{content}&signature={Base64Url(signature)}&key_id={keyId}";
    }

    private const string RewardContent =
        "ad_network=5450213213286189855&ad_unit=1234567890&custom_data=42&reward_amount=1&reward_item=episode" +
        "&timestamp=1760600000000&transaction_id=123456789&user_id=0b8c0f6e-0000-4000-8000-000000000001";

    [Test]
    public async Task AdMobCallback_WithValidSignature_IsReadBack()
    {
        var (verifier, key) = AdMob();
        using var _ = key;

        var reward = await verifier.VerifyAsync(AdMobQuery(key, RewardContent), CancellationToken.None);

        reward.TransactionId.ShouldBe("123456789");
        reward.UserId.ShouldBe("0b8c0f6e-0000-4000-8000-000000000001");
        reward.CustomData.ShouldBe("42");
        reward.Timestamp.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1760600000000));
    }

    [Test]
    public async Task AdMobCallback_IsRejectedWhenTamperedOrSignedByAnUnknownKey()
    {
        var (verifier, key) = AdMob();
        using var _ = key;
        var valid = AdMobQuery(key, RewardContent);

        await Should.ThrowAsync<StoreVerificationException>(() => verifier.VerifyAsync(valid.Replace("custom_data=42", "custom_data=43"), CancellationToken.None));
        await Should.ThrowAsync<StoreVerificationException>(() => verifier.VerifyAsync(AdMobQuery(key, RewardContent, keyId: 1), CancellationToken.None));
        await Should.ThrowAsync<StoreVerificationException>(() => verifier.VerifyAsync("?transaction_id=1&user_id=x", CancellationToken.None));
    }

    [Test]
    public void GooglePlayResponse_TakesTheLatestLineItem_AndAccountOfTheBuyer()
    {
        using var document = JsonDocument.Parse("""
            {
              "subscriptionState": "SUBSCRIPTION_STATE_ACTIVE",
              "linkedPurchaseToken": "old-token",
              "acknowledgementState": "ACKNOWLEDGEMENT_STATE_PENDING",
              "externalAccountIdentifiers": { "obfuscatedExternalAccountId": "acc-1" },
              "testPurchase": {},
              "lineItems": [
                { "productId": "luma.vip.week", "expiryTime": "2026-10-10T00:00:00Z" },
                { "productId": "luma.vip.month", "expiryTime": "2026-11-16T10:00:00.5Z", "autoRenewingPlan": { "autoRenewEnabled": true } }
              ]
            }
            """);

        var state = GooglePlayClient.Read("new-token", document.RootElement);

        state.ShouldBe(new(PurchaseToken: "new-token", LinkedPurchaseToken: "old-token", State: "SUBSCRIPTION_STATE_ACTIVE",
            ProductId: "luma.vip.month", ExpiresAt: DateTimeOffset.Parse("2026-11-16T10:00:00.5Z"), AutoRenew: true,
            Acknowledged: false, AccountId: "acc-1", IsTest: true));
    }
}
