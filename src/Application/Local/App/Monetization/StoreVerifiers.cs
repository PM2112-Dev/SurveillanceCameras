using System.Security.Cryptography;
using System.Text;

namespace SurveillanceCameras.Application.Local.App.Monetization;

/// <summary>Giao dịch App Store đã kiểm chữ ký (JWSTransactionDecodedPayload).</summary>
public sealed record AppStoreTransaction(
    string TransactionId,
    string OriginalTransactionId,
    string ProductId,
    string? BundleId,
    DateTimeOffset? PurchasedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt,
    Guid? AppAccountToken,
    bool IsSandbox);

/// <summary>Thông tin gia hạn App Store đã kiểm chữ ký (JWSRenewalInfoDecodedPayload).</summary>
public sealed record AppStoreRenewal(bool AutoRenew, bool IsInBillingRetry, DateTimeOffset? GracePeriodExpiresAt);

/// <summary>App Store Server Notification V2 đã kiểm chữ ký.</summary>
public sealed record AppStoreNotification(
    string NotificationId,
    string Type,
    string? Subtype,
    string? BundleId,
    bool IsSandbox,
    AppStoreTransaction? Transaction,
    AppStoreRenewal? Renewal);

public interface IAppStoreVerifier
{
    /// <summary>Kiểm chữ ký giao dịch do StoreKit 2 trả về trên máy (chuỗi chứng chỉ tới Apple Root CA G3) và đúng app.</summary>
    /// <exception cref="Common.Exceptions.StoreVerificationException">Chữ ký sai, sai app hoặc chưa cấu hình.</exception>
    AppStoreTransaction VerifyTransaction(string? signedTransaction);

    AppStoreRenewal VerifyRenewal(string? signedRenewalInfo);

    AppStoreNotification VerifyNotification(string? signedPayload);
}

/// <summary>Trạng thái gói Google Play (purchases.subscriptionsv2).</summary>
/// <param name="State">Giá trị <c>subscriptionState</c> của Google, vd. <c>SUBSCRIPTION_STATE_ACTIVE</c>.</param>
/// <param name="AccountId"><c>obfuscatedExternalAccountId</c> app gắn lúc mua (mã tài khoản trên máy chủ này).</param>
public sealed record GooglePlaySubscription(
    string PurchaseToken,
    string? LinkedPurchaseToken,
    string State,
    string? ProductId,
    DateTimeOffset? ExpiresAt,
    bool AutoRenew,
    bool Acknowledged,
    string? AccountId,
    bool IsTest);

public interface IGooglePlayClient
{
    /// <exception cref="Common.Exceptions.StoreVerificationException">Token không tồn tại, sai app, hoặc chưa cấu hình.</exception>
    Task<GooglePlaySubscription> GetSubscriptionAsync(string? purchaseToken, CancellationToken cancellationToken);

    /// <summary>Xác nhận đã cấp quyền; Google tự hoàn tiền gói chưa được xác nhận sau 3 ngày.</summary>
    Task AcknowledgeAsync(string productId, string purchaseToken, CancellationToken cancellationToken);

    /// <summary>Request đẩy thông báo từ Pub/Sub có mang đúng mã bí mật đã cấu hình trong URL push.</summary>
    bool IsNotificationTokenValid(string? token);
}

/// <summary>Callback xác nhận lượt xem quảng cáo có thưởng của AdMob (server-side verification).</summary>
public sealed record AdMobReward(string TransactionId, string? UserId, string? CustomData, string? AdUnit, DateTimeOffset Timestamp);

public interface IAdMobRewardVerifier
{
    /// <summary>Kiểm chữ ký callback SSV trên chuỗi query nguyên bản (<c>?ad_network=…&amp;signature=…&amp;key_id=…</c>).</summary>
    /// <exception cref="Common.Exceptions.StoreVerificationException">Chữ ký sai hoặc thiếu tham số.</exception>
    Task<AdMobReward> VerifyAsync(string? queryString, CancellationToken cancellationToken);
}

public static class StoreAccount
{
    /// <summary>
    /// Mã tài khoản gắn vào giao dịch lúc mua (<c>appAccountToken</c> của StoreKit, <c>obfuscatedAccountId</c> của Google
    /// Play) để biết gói thuộc tài khoản nào. ID người dùng của Identity vốn là GUID thì dùng luôn.
    /// </summary>
    public static Guid TokenFor(string userId)
    {
        if (Guid.TryParse(userId, out var id)) return id;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(userId));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50); // UUID phiên bản 5 (dựa trên hash)
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }

    /// <summary>ID người dùng có mã tài khoản <paramref name="token"/>, nếu ID đó là GUID.</summary>
    public static string UserIdFor(Guid token) => token.ToString();
}
