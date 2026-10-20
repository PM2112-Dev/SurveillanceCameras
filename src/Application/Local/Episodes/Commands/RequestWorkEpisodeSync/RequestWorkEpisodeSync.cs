using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;

namespace SurveillanceCameras.Application.Local.Episodes.Commands.RequestWorkEpisodeSync;

/// <summary>Đồng bộ lại mục lục (lấy chương mới của truyện đang ra) — chạy nền qua Kafka, chương đã có được giữ nguyên.</summary>
public record RequestWorkEpisodeSyncCommand(int WorkId) : IRequest;

public class RequestWorkEpisodeSyncCommandHandler : IRequestHandler<RequestWorkEpisodeSyncCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public RequestWorkEpisodeSyncCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(RequestWorkEpisodeSyncCommand request, CancellationToken cancellationToken)
    {
        var story = await _context.Works
            .FirstOrDefaultAsync(x => x.Id == request.WorkId && x.CreatedBy == _user.Id && x.Type == WorkType.Story, cancellationToken);

        Guard.Against.NotFound(request.WorkId, story);

        story.EpisodeSyncError = null;
        story.AddIntegrationEvent(new StoryChaptersSyncRequestedIntegrationEvent { StoryId = story.Id, Page = 1 }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
