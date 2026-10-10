using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;

namespace SurveillanceCameras.Application.Local.Episodes.Commands.RequestEpisodeEdit;

/// <summary>AI biên tập lại một chương (kể cả chương đã biên tập) — chạy nền qua Kafka.</summary>
public record RequestEpisodeEditCommand(int ChapterId) : IRequest;

public class RequestEpisodeEditCommandHandler : IRequestHandler<RequestEpisodeEditCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public RequestEpisodeEditCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(RequestEpisodeEditCommand request, CancellationToken cancellationToken)
    {
        var chapter = await _context.Episodes
            .FirstOrDefaultAsync(x => x.Id == request.ChapterId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.ChapterId, chapter);

        if (string.IsNullOrWhiteSpace(chapter.ContentRaw))
        {
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.ChapterId), "Chương chưa có nội dung gốc để biên tập.")]);
        }

        chapter.Error = null;
        chapter.AddIntegrationEvent(new ChapterEditRequestedIntegrationEvent { ChapterId = chapter.Id, Force = true },
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
