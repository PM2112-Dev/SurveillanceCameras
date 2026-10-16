using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.App.Monetization.Commands;

/// <summary>
/// App gửi các giao dịch StoreKit 2 (JWS) sau khi mua hoặc khi khôi phục; máy chủ kiểm chữ ký rồi gắn gói vào người đang
/// đăng nhập.
/// </summary>
public record VerifyAppStorePurchaseCommand(IReadOnlyList<string> SignedTransactions) : IRequest;

public class VerifyAppStorePurchaseCommandHandler : IRequestHandler<VerifyAppStorePurchaseCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IAppStoreVerifier _verifier;
    private readonly TimeProvider _timeProvider;

    public VerifyAppStorePurchaseCommandHandler(IApplicationDbContext context, IUser user, IAppStoreVerifier verifier,
        TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _verifier = verifier;
        _timeProvider = timeProvider;
    }

    public async Task Handle(VerifyAppStorePurchaseCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.RequireId();
        if (request.SignedTransactions.Count == 0) throw new StoreVerificationException("Thiếu giao dịch App Store.");

        // Kiểm hết trước khi ghi: một giao dịch giả thì không ghi gì cả.
        var transactions = request.SignedTransactions.Select(_verifier.VerifyTransaction)
            // Nhiều giao dịch của cùng một gói (các kỳ gia hạn): lấy kỳ mới nhất.
            .GroupBy(x => x.OriginalTransactionId)
            .Select(g => g.OrderByDescending(x => x.ExpiresAt ?? DateTimeOffset.MinValue).First())
            .ToList();

        var now = _timeProvider.GetUtcNow();
        foreach (var transaction in transactions)
        {
            var subscription = await _context.AppSubscriptions.FirstOrDefaultAsync(x => x.Source == SubscriptionSource.AppStore
                && x.ExternalId == transaction.OriginalTransactionId, cancellationToken);
            StoreSubscriptions.EnsureOwner(userId, transaction.AppAccountToken, subscription);

            if (subscription is null)
            {
                subscription = new AppSubscription { UserId = userId };
                _context.AppSubscriptions.Add(subscription);
            }

            var plan = await _context.PlanCodeAsync(StoreKind.AppStore, transaction.ProductId, cancellationToken);
            StoreSubscriptions.Apply(subscription, transaction, null, plan, now);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>App gửi purchase token Google Play sau khi mua hoặc khi khôi phục; máy chủ hỏi Google rồi gắn gói vào người đang đăng nhập.</summary>
public record VerifyGooglePlayPurchaseCommand(IReadOnlyList<string> PurchaseTokens) : IRequest;

public class VerifyGooglePlayPurchaseCommandHandler : IRequestHandler<VerifyGooglePlayPurchaseCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IGooglePlayClient _googlePlay;
    private readonly TimeProvider _timeProvider;

    public VerifyGooglePlayPurchaseCommandHandler(IApplicationDbContext context, IUser user, IGooglePlayClient googlePlay,
        TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _googlePlay = googlePlay;
        _timeProvider = timeProvider;
    }

    public async Task Handle(VerifyGooglePlayPurchaseCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.RequireId();
        var tokens = request.PurchaseTokens.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (tokens.Count == 0) throw new StoreVerificationException("Thiếu purchase token của Google Play.");

        foreach (var token in tokens)
        {
            var state = await _googlePlay.GetSubscriptionAsync(token, cancellationToken);
            var subscription = await _context.FindGooglePlayAsync(state, cancellationToken);
            Guid? account = Guid.TryParse(state.AccountId, out var parsed) ? parsed : null;
            StoreSubscriptions.EnsureOwner(userId, account, subscription);

            if (subscription is null)
            {
                subscription = new AppSubscription { UserId = userId };
                _context.AppSubscriptions.Add(subscription);
            }

            var plan = await _context.PlanCodeAsync(StoreKind.GooglePlay, state.ProductId, cancellationToken);
            StoreSubscriptions.Apply(subscription, state, plan, _timeProvider.GetUtcNow());
            await _context.SaveChangesAsync(cancellationToken);

            // Chỉ xác nhận với Google sau khi đã ghi quyền: lỗi giữa chừng thì Google hoàn tiền thay vì người dùng mất tiền.
            if (!state.Acknowledged && StoreSubscriptions.GrantsAccess(state) && state.ProductId is not null)
                await _googlePlay.AcknowledgeAsync(state.ProductId, state.PurchaseToken, cancellationToken);
        }
    }
}
