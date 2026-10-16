using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.App.Monetization.Commands;

/// <summary>
/// App Store Server Notification V2 (gia hạn, tắt gia hạn, lỗi thanh toán, hết hạn, hoàn tiền…). Mỗi thông báo xử lý một
/// lần; thông báo cho gói chưa ghi nhận thì gắn vào tài khoản theo <c>appAccountToken</c>.
/// </summary>
public record HandleAppStoreNotificationCommand(string? SignedPayload) : IRequest;

public class HandleAppStoreNotificationCommandHandler : IRequestHandler<HandleAppStoreNotificationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAppStoreVerifier _verifier;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<HandleAppStoreNotificationCommandHandler> _logger;

    public HandleAppStoreNotificationCommandHandler(IApplicationDbContext context, IAppStoreVerifier verifier,
        TimeProvider timeProvider, ILogger<HandleAppStoreNotificationCommandHandler> logger)
    {
        _context = context;
        _verifier = verifier;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Handle(HandleAppStoreNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = _verifier.VerifyNotification(request.SignedPayload);
        if (await _context.AppStoreEvents.AnyAsync(x => x.Store == StoreKind.AppStore
                && x.NotificationId == notification.NotificationId, cancellationToken)) return;

        var now = _timeProvider.GetUtcNow();
        var transaction = notification.Transaction;
        var log = new AppStoreEvent
        {
            Store = StoreKind.AppStore,
            NotificationId = notification.NotificationId,
            Type = notification.Subtype is null ? notification.Type : $"{notification.Type}/{notification.Subtype}",
            ExternalId = transaction?.OriginalTransactionId,
            Payload = request.SignedPayload ?? "",
            ProcessedAt = now
        };
        _context.AppStoreEvents.Add(log);

        if (transaction is not null)
        {
            var subscription = await _context.AppSubscriptions.FirstOrDefaultAsync(x => x.Source == SubscriptionSource.AppStore
                && x.ExternalId == transaction.OriginalTransactionId, cancellationToken);
            if (subscription is null && transaction.AppAccountToken is { } token)
            {
                subscription = new AppSubscription { UserId = StoreAccount.UserIdFor(token) };
                _context.AppSubscriptions.Add(subscription);
            }

            if (subscription is null)
            {
                // Mua từ bản app chưa gắn appAccountToken: chờ app gửi giao dịch lên (mua xong hoặc Khôi phục).
                log.Error = "Chưa biết gói thuộc tài khoản nào.";
            }
            else
            {
                var plan = await _context.PlanCodeAsync(StoreKind.AppStore, transaction.ProductId, cancellationToken);
                StoreSubscriptions.Apply(subscription, transaction, notification.Renewal, plan, now);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("App Store notification {Type} for {Transaction}: {Result}", log.Type, log.ExternalId, log.Error ?? "applied");
    }
}

/// <summary>
/// Real-time developer notification của Google Play (qua Pub/Sub push). Chỉ dùng thông báo như tín hiệu: trạng thái thật
/// luôn hỏi lại Google theo purchase token.
/// </summary>
/// <param name="Data">Trường <c>message.data</c> (base64) của Pub/Sub.</param>
public record HandleGooglePlayNotificationCommand(string? MessageId, string? Data) : IRequest;

public class HandleGooglePlayNotificationCommandHandler : IRequestHandler<HandleGooglePlayNotificationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IGooglePlayClient _googlePlay;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<HandleGooglePlayNotificationCommandHandler> _logger;

    public HandleGooglePlayNotificationCommandHandler(IApplicationDbContext context, IGooglePlayClient googlePlay,
        TimeProvider timeProvider, ILogger<HandleGooglePlayNotificationCommandHandler> logger)
    {
        _context = context;
        _googlePlay = googlePlay;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Handle(HandleGooglePlayNotificationCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.MessageId) || string.IsNullOrWhiteSpace(request.Data))
            throw new StoreVerificationException("Thông báo Pub/Sub thiếu messageId hoặc data.");
        if (await _context.AppStoreEvents.AnyAsync(x => x.Store == StoreKind.GooglePlay
                && x.NotificationId == request.MessageId, cancellationToken)) return;

        string json;
        try { json = Encoding.UTF8.GetString(Convert.FromBase64String(request.Data)); }
        catch (FormatException ex) { throw new StoreVerificationException("Dữ liệu thông báo Google Play không phải base64.", ex); }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var now = _timeProvider.GetUtcNow();
        var log = new AppStoreEvent { Store = StoreKind.GooglePlay, NotificationId = request.MessageId, Payload = json, ProcessedAt = now };
        _context.AppStoreEvents.Add(log);

        if (root.TryGetProperty("subscriptionNotification", out var notification))
        {
            var token = notification.GetProperty("purchaseToken").GetString();
            log.Type = notification.TryGetProperty("notificationType", out var type) ? $"subscription/{type.GetInt32()}" : "subscription";
            log.ExternalId = token;

            var state = await _googlePlay.GetSubscriptionAsync(token, cancellationToken);
            var subscription = await _context.FindGooglePlayAsync(state, cancellationToken);
            if (subscription is null && Guid.TryParse(state.AccountId, out var account))
            {
                subscription = new AppSubscription { UserId = StoreAccount.UserIdFor(account) };
                _context.AppSubscriptions.Add(subscription);
            }

            if (subscription is null) log.Error = "Chưa biết gói thuộc tài khoản nào.";
            else
            {
                var plan = await _context.PlanCodeAsync(StoreKind.GooglePlay, state.ProductId, cancellationToken);
                StoreSubscriptions.Apply(subscription, state, plan, now);
            }

            await _context.SaveChangesAsync(cancellationToken);
            if (subscription is not null && !state.Acknowledged && StoreSubscriptions.GrantsAccess(state) && state.ProductId is not null)
                await _googlePlay.AcknowledgeAsync(state.ProductId, state.PurchaseToken, cancellationToken);
        }
        else if (root.TryGetProperty("voidedPurchaseNotification", out var voided))
        {
            var token = voided.GetProperty("purchaseToken").GetString();
            log.Type = "voided";
            log.ExternalId = token;
            var subscription = await _context.AppSubscriptions.FirstOrDefaultAsync(x => x.Source == SubscriptionSource.GooglePlay
                && (x.ExternalId == token || x.PreviousExternalId == token), cancellationToken);
            if (subscription is null) log.Error = "Không có gói nào với purchase token này.";
            else
            {
                subscription.Status = SubscriptionStatus.Revoked;
                if (subscription.ExpiresAt > now) subscription.ExpiresAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            log.Type = root.TryGetProperty("testNotification", out _) ? "test" : "other";
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Google Play notification {Type} for {Token}: {Result}", log.Type, log.ExternalId, log.Error ?? "applied");
    }
}
