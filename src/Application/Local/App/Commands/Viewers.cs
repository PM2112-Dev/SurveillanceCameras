using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.App.Commands;

/// <summary>Xoá mọi dữ liệu app của một người xem (trước khi xoá tài khoản).</summary>
public record DeleteAppViewerDataCommand(string UserId) : IRequest;

public class DeleteAppViewerDataCommandHandler : IRequestHandler<DeleteAppViewerDataCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteAppViewerDataCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteAppViewerDataCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var bookmarks = await _context.AppBookmarks.Where(x => x.CreatedBy == userId).ToListAsync(cancellationToken);
        _context.AppBookmarks.RemoveRange(bookmarks);
        _context.AppWatchHistories.RemoveRange(await _context.AppWatchHistories.Where(x => x.CreatedBy == userId).ToListAsync(cancellationToken));
        _context.AppComments.RemoveRange(await _context.AppComments.Where(x => x.CreatedBy == userId).ToListAsync(cancellationToken));
        _context.AppEpisodeUnlocks.RemoveRange(await _context.AppEpisodeUnlocks.Where(x => x.CreatedBy == userId).ToListAsync(cancellationToken));
        _context.AppSubscriptions.RemoveRange(await _context.AppSubscriptions.Where(x => x.UserId == userId).ToListAsync(cancellationToken));

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var bookmark in bookmarks)
            await _context.AdjustBookmarkCountAsync(bookmark.WorkId, -1, cancellationToken);
    }
}

/// <summary>
/// Quản trị viên cấp/gia hạn VIP cho một người xem tới <paramref name="ExpiresAt"/>; thời điểm đã qua nghĩa là thu hồi.
/// </summary>
public record SetAppViewerVipCommand(string UserId, string? Plan, DateTimeOffset ExpiresAt) : IRequest;

public class SetAppViewerVipCommandHandler : IRequestHandler<SetAppViewerVipCommand>
{
    private readonly IApplicationDbContext _context;

    public SetAppViewerVipCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(SetAppViewerVipCommand request, CancellationToken cancellationToken)
    {
        // Chỉ sửa gói do quản trị viên cấp; gói mua qua cửa hàng do cửa hàng quyết định.
        var subscription = await _context.AppSubscriptions
            .FirstOrDefaultAsync(x => x.UserId == request.UserId && x.Source == SubscriptionSource.Admin, cancellationToken);

        if (subscription is null)
        {
            subscription = new AppSubscription { UserId = request.UserId, Source = SubscriptionSource.Admin };
            _context.AppSubscriptions.Add(subscription);
        }

        subscription.Plan = request.Plan;
        subscription.ExpiresAt = request.ExpiresAt;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
