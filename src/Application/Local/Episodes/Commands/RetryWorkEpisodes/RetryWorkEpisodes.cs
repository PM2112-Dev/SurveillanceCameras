using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Episodes.Commands.RetryWorkEpisodes;

/// <summary>
/// Xử lý lại các chương còn dở của truyện: tải lại chương tải lỗi (hoặc chưa tải), biên tập lại chương biên tập lỗi
/// (hoặc đã tải mà chưa biên tập — vd. lúc đó chưa có prompt). Dùng sau khi sửa cấu hình, thêm prompt hoặc có lại quota.
/// </summary>
/// <returns>Số chương được đưa vào hàng đợi.</returns>
public record RetryWorkEpisodesCommand(int WorkId) : IRequest<int>;

public class RetryWorkEpisodesCommandHandler : IRequestHandler<RetryWorkEpisodesCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public RetryWorkEpisodesCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(RetryWorkEpisodesCommand request, CancellationToken cancellationToken)
    {
        var story = await _context.Works
            .FirstOrDefaultAsync(x => x.Id == request.WorkId && x.CreatedBy == _user.Id && x.Type == WorkType.Story, cancellationToken);

        Guard.Against.NotFound(request.WorkId, story);

        // Chỉ lấy Id + trạng thái: không nạp nội dung của hàng nghìn chương.
        var chapters = await _context.Episodes
            .Where(x => x.WorkId == story.Id && x.Status != EpisodeStatus.Edited)
            .Select(x => new { x.Id, x.Status })
            .ToListAsync(cancellationToken);

        // Gắn sự kiện vào bản ghi truyện (OutboxInterceptor lấy sự kiện từ mọi entity đang được theo dõi).
        foreach (var chapter in chapters)
        {
            IntegrationEvent evt = chapter.Status is EpisodeStatus.Listed or EpisodeStatus.FetchFailed
                ? new ChapterFetchRequestedIntegrationEvent { ChapterId = chapter.Id }
                : new ChapterEditRequestedIntegrationEvent { ChapterId = chapter.Id };
            story.AddIntegrationEvent(evt, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return chapters.Count;
    }
}
