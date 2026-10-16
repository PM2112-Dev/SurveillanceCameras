using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.App.Monetization.Commands;

/// <summary>Ghi trạng thái gói của App Store / Google Play vào <see cref="AppSubscription"/>.</summary>
internal static class StoreSubscriptions
{
    public const string OwnedByAnotherAccount = "Gói này đã được mua bằng một tài khoản LUMA khác. Đăng nhập tài khoản đó để dùng.";

    /// <summary>Mã gói (<see cref="AppSubscriptionPlan"/>.Code) có product ID này, nếu quản trị viên đã khai báo.</summary>
    public static async Task<string?> PlanCodeAsync(this IApplicationDbContext context, StoreKind store, string? productId,
        CancellationToken cancellationToken)
        => productId is null ? null
            : await context.AppSubscriptionPlans.AsNoTracking()
                .Where(x => store == StoreKind.AppStore ? x.AppleProductId == productId : x.GoogleProductId == productId)
                .Select(x => x.Code)
                .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Áp giao dịch App Store vào gói. Hạn chỉ lùi lại khi bị hoàn tiền/thu hồi: thông báo cũ tới muộn không làm mất kỳ
    /// đã gia hạn.
    /// </summary>
    public static void Apply(AppSubscription subscription, AppStoreTransaction transaction, AppStoreRenewal? renewal,
        string? plan, DateTimeOffset now)
    {
        subscription.Source = SubscriptionSource.AppStore;
        subscription.ExternalId = transaction.OriginalTransactionId;
        subscription.ProductId = transaction.ProductId;
        subscription.Plan = plan ?? subscription.Plan;
        subscription.IsSandbox = transaction.IsSandbox;
        subscription.VerifiedAt = now;
        if (renewal is not null) subscription.AutoRenew = renewal.AutoRenew;

        if (transaction.RevokedAt is { } revokedAt)
        {
            subscription.Status = SubscriptionStatus.Revoked;
            subscription.ExpiresAt = revokedAt < now ? revokedAt : now;
            return;
        }

        var expires = transaction.ExpiresAt ?? now;
        if (subscription.Status != SubscriptionStatus.Revoked && subscription.ExpiresAt > expires) expires = subscription.ExpiresAt;
        // Gia hạn bị lỗi thanh toán: Apple vẫn cho dùng trong thời gian ân hạn.
        if (renewal is { IsInBillingRetry: true, GracePeriodExpiresAt: { } grace } && grace > expires) expires = grace;
        subscription.ExpiresAt = expires;

        subscription.Status = expires > now
            ? renewal?.IsInBillingRetry == true ? SubscriptionStatus.GracePeriod
            : renewal is null || renewal.AutoRenew ? SubscriptionStatus.Active : SubscriptionStatus.Canceled
            : renewal?.IsInBillingRetry == true ? SubscriptionStatus.OnHold : SubscriptionStatus.Expired;
    }

    /// <summary>Áp trạng thái Google Play vào gói. Tạm khoá / tạm dừng / chờ thanh toán thì mất quyền ngay.</summary>
    public static void Apply(AppSubscription subscription, GooglePlaySubscription state, string? plan, DateTimeOffset now)
    {
        subscription.Source = SubscriptionSource.GooglePlay;
        if (subscription.ExternalId != null && subscription.ExternalId != state.PurchaseToken)
            subscription.PreviousExternalId = subscription.ExternalId;
        else if (state.LinkedPurchaseToken != null) subscription.PreviousExternalId = state.LinkedPurchaseToken;
        subscription.ExternalId = state.PurchaseToken;
        subscription.ProductId = state.ProductId ?? subscription.ProductId;
        subscription.Plan = plan ?? subscription.Plan;
        subscription.AutoRenew = state.AutoRenew;
        subscription.IsSandbox = state.IsTest;
        subscription.VerifiedAt = now;

        var expires = state.ExpiresAt ?? now;
        (subscription.Status, subscription.ExpiresAt) = state.State switch
        {
            "SUBSCRIPTION_STATE_ACTIVE" => (SubscriptionStatus.Active, expires),
            "SUBSCRIPTION_STATE_CANCELED" => (SubscriptionStatus.Canceled, expires),
            "SUBSCRIPTION_STATE_IN_GRACE_PERIOD" => (SubscriptionStatus.GracePeriod, expires),
            "SUBSCRIPTION_STATE_ON_HOLD" => (SubscriptionStatus.OnHold, Earlier(expires, now)),
            "SUBSCRIPTION_STATE_PAUSED" => (SubscriptionStatus.Paused, Earlier(expires, now)),
            "SUBSCRIPTION_STATE_PENDING" => (SubscriptionStatus.Pending, Earlier(expires, now)),
            _ => (SubscriptionStatus.Expired, Earlier(expires, now))
        };
    }

    private static DateTimeOffset Earlier(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;

    public static bool GrantsAccess(GooglePlaySubscription state)
        => state.State is "SUBSCRIPTION_STATE_ACTIVE" or "SUBSCRIPTION_STATE_CANCELED" or "SUBSCRIPTION_STATE_IN_GRACE_PERIOD";

    /// <summary>
    /// Gói Google Play theo purchase token: dòng đang giữ token này, hoặc dòng của token cũ mà token này thay thế
    /// (đổi gói, mua lại).
    /// </summary>
    public static async Task<AppSubscription?> FindGooglePlayAsync(this IApplicationDbContext context, GooglePlaySubscription state,
        CancellationToken cancellationToken)
        => await context.AppSubscriptions.FirstOrDefaultAsync(x => x.Source == SubscriptionSource.GooglePlay
               && x.ExternalId == state.PurchaseToken, cancellationToken)
           ?? (state.LinkedPurchaseToken is null ? null
               : await context.AppSubscriptions.FirstOrDefaultAsync(x => x.Source == SubscriptionSource.GooglePlay
                   && x.ExternalId == state.LinkedPurchaseToken, cancellationToken));

    /// <summary>Chủ gói theo mã tài khoản gắn lúc mua; gói đã ghi cho người khác thì không được nhận.</summary>
    public static void EnsureOwner(string userId, Guid? accountToken, AppSubscription? existing)
    {
        if (accountToken is { } token && token != StoreAccount.TokenFor(userId))
            throw new AppRequestException(409, OwnedByAnotherAccount, "purchase_owned_by_other_account");
        if (existing is not null && existing.UserId != userId)
            throw new AppRequestException(409, OwnedByAnotherAccount, "purchase_owned_by_other_account");
    }
}
