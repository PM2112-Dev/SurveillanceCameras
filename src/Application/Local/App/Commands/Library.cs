using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.App.Commands;

/// <summary>Lưu hoặc bỏ lưu một phim cho người đang đăng nhập. Gọi lặp lại không sinh bản trùng.</summary>
public record SetAppBookmarkCommand(int MovieId, bool Saved) : IRequest;

public class SetAppBookmarkCommandHandler : IRequestHandler<SetAppBookmarkCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SetAppBookmarkCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(SetAppBookmarkCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.RequireId();
        var existing = await _context.AppBookmarks
            .FirstOrDefaultAsync(x => x.CreatedBy == userId && x.WorkId == request.MovieId, cancellationToken);

        if (!request.Saved)
        {
            if (existing is null) return;
            _context.AppBookmarks.Remove(existing);
        }
        else
        {
            if (existing is not null) return;
            if (!await _context.PublishedMovies().AnyAsync(x => x.Id == request.MovieId, cancellationToken))
                throw new NotFoundException(request.MovieId.ToString(), "Movie");
            _context.AppBookmarks.Add(new AppBookmark { WorkId = request.MovieId, CreatedBy = userId });
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Cùng lúc có request khác của người này đã lưu/bỏ lưu phim (chỉ mục duy nhất, hoặc dòng đã bị xoá):
            // request đó đã tính lượt rồi.
            return;
        }

        await _context.AdjustBookmarkCountAsync(request.MovieId, request.Saved ? 1 : -1, cancellationToken);
    }
}

/// <summary>Ghi tiến độ xem một phim của người đang đăng nhập (tập đang xem và vị trí trong tập).</summary>
public record SaveAppWatchProgressCommand : IRequest
{
    public int MovieId { get; init; }

    public int EpisodeNumber { get; init; }

    public int PositionSeconds { get; init; }

    public int DurationSeconds { get; init; }

    /// <summary>Thời điểm xem trên thiết bị; bỏ trống thì lấy giờ máy chủ.</summary>
    public DateTimeOffset? WatchedAt { get; init; }
}

public class SaveAppWatchProgressCommandValidator : AbstractValidator<SaveAppWatchProgressCommand>
{
    public SaveAppWatchProgressCommandValidator()
    {
        RuleFor(x => x.EpisodeNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PositionSeconds).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationSeconds).GreaterThanOrEqualTo(0);
    }
}

public class SaveAppWatchProgressCommandHandler : IRequestHandler<SaveAppWatchProgressCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public SaveAppWatchProgressCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task Handle(SaveAppWatchProgressCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.RequireId();
        var now = _timeProvider.GetUtcNow();
        // Đồng hồ thiết bị chạy nhanh không được đẩy mục này lên đầu lịch sử mãi.
        var watchedAt = request.WatchedAt is { } at && at <= now ? at : now;

        var entry = await _context.AppWatchHistories
            .FirstOrDefaultAsync(x => x.CreatedBy == userId && x.WorkId == request.MovieId, cancellationToken);

        if (entry is null)
        {
            if (!await _context.PublishedMovies().AnyAsync(x => x.Id == request.MovieId, cancellationToken))
                throw new NotFoundException(request.MovieId.ToString(), "Movie");

            entry = new AppWatchHistory { WorkId = request.MovieId, CreatedBy = userId };
            _context.AppWatchHistories.Add(entry);
        }
        // Thiết bị khác đã ghi tiến độ mới hơn (vd. đồng bộ lịch sử cũ sau khi đăng nhập) thì giữ bản mới.
        else if (entry.WatchedAt > watchedAt)
        {
            return;
        }

        entry.EpisodeNumber = request.EpisodeNumber;
        entry.PositionSeconds = request.PositionSeconds;
        entry.DurationSeconds = request.DurationSeconds;
        entry.WatchedAt = watchedAt;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Xoá một phim khỏi lịch sử xem của người đang đăng nhập.</summary>
public record RemoveAppWatchHistoryCommand(int MovieId) : IRequest;

public class RemoveAppWatchHistoryCommandHandler : IRequestHandler<RemoveAppWatchHistoryCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public RemoveAppWatchHistoryCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(RemoveAppWatchHistoryCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.RequireId();
        var entry = await _context.AppWatchHistories
            .FirstOrDefaultAsync(x => x.CreatedBy == userId && x.WorkId == request.MovieId, cancellationToken);
        if (entry is null) return;

        _context.AppWatchHistories.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
