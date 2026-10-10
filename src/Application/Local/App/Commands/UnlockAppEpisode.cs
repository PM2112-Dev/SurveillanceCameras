using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.App.Commands;

/// <summary>
/// Người đang đăng nhập mở một tập thuộc nhóm "mở bằng quảng cáo" (gọi sau khi xem xong quảng cáo). Tập miễn phí hoặc đã
/// mở thì trả về luôn; tập chỉ dành cho VIP thì từ chối.
/// </summary>
public record UnlockAppEpisodeCommand(int EpisodeId) : IRequest<AppEpisodeDto>;

public class UnlockAppEpisodeCommandHandler : IRequestHandler<UnlockAppEpisodeCommand, AppEpisodeDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;
    private readonly AppOptions _options;

    public UnlockAppEpisodeCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider,
        IOptions<AppOptions> options)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<AppEpisodeDto> Handle(UnlockAppEpisodeCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.RequireId();
        var episode = await _context.Episodes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.EpisodeId
                                      && _context.PublishedMovies().Any(w => w.Id == x.WorkId), cancellationToken);
        Guard.Against.NotFound(request.EpisodeId, episode);

        var isVip = await _context.IsVipAsync(userId, _timeProvider.GetUtcNow(), cancellationToken);
        var unlocked = await _context.AppEpisodeUnlocks
            .AnyAsync(x => x.CreatedBy == userId && x.EpisodeId == episode.Id, cancellationToken);

        var access = AppAccess.For(_options, episode.EpisodeNumber, isVip, unlocked);
        if (access == AppAccess.Vip) throw new Common.Exceptions.ForbiddenAccessException();

        if (access == AppAccess.Ads)
        {
            _context.AppEpisodeUnlocks.Add(new AppEpisodeUnlock { EpisodeId = episode.Id, CreatedBy = userId });
            await _context.SaveChangesAsync(cancellationToken);
            access = AppAccess.Free;
        }

        return episode.ToAppEpisode(access);
    }
}
