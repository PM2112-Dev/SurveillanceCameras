using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Episodes.Commands.RequestTytChapterUpdate;

/// <summary>Cập nhật lại một chương đã đăng lên TYT bằng nội dung hiện tại (chạy nền qua Kafka).</summary>
public record RequestTytChapterUpdateCommand(int ChapterId) : IRequest;

public class RequestTytChapterUpdateCommandHandler : IRequestHandler<RequestTytChapterUpdateCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public RequestTytChapterUpdateCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(RequestTytChapterUpdateCommand request, CancellationToken cancellationToken)
    {
        var chapter = await _context.Episodes
            .FirstOrDefaultAsync(x => x.Id == request.ChapterId && x.Work!.CreatedBy == _user.Id && x.Work.Type == WorkType.Story, cancellationToken);
        Guard.Against.NotFound(request.ChapterId, chapter);

        if (!chapter.IsUploaded)
        {
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.ChapterId), "Chương chưa đăng lên TYT — dùng “Đăng chương lên TYT” trước.")]);
        }

        if (chapter.Status != EpisodeStatus.Edited || string.IsNullOrWhiteSpace(chapter.ContentEdit))
        {
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.ChapterId), "Chương chưa có nội dung đã biên tập để cập nhật lên TYT.")]);
        }

        chapter.TytUploadError = null;
        chapter.AddIntegrationEvent(new ChapterTytUploadRequestedIntegrationEvent { ChapterId = chapter.Id, Force = true }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
