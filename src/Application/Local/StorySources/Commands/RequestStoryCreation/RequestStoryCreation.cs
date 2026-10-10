using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.StorySources.IntegrationEvents;

namespace SurveillanceCameras.Application.Local.StorySources.Commands.RequestStoryCreation;

/// <summary>
/// Yêu cầu dùng AI tạo truyện từ nguồn truyện (chạy nền qua Kafka) — cho nguồn đồng bộ trước khi có tính năng này,
/// hoặc chạy lại sau khi lỗi (hết quota, chưa khai báo prompt/AI…).
/// </summary>
public record RequestStoryCreationCommand(int StorySourceId) : IRequest;

public class RequestStoryCreationCommandHandler : IRequestHandler<RequestStoryCreationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public RequestStoryCreationCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(RequestStoryCreationCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StorySources
            .FirstOrDefaultAsync(x => x.Id == request.StorySourceId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.StorySourceId, entity);

        if (await _context.Works.AnyAsync(x => x.StorySourceId == entity.Id, cancellationToken))
        {
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.StorySourceId), "Nguồn truyện này đã có truyện.")]);
        }

        entity.StoryCreationError = null;
        entity.AddIntegrationEvent(new StorySourceCreatedIntegrationEvent
        {
            StorySourceId = entity.Id,
            Title = entity.Title,
            WebSourceId = entity.WebSourceId,
            LinkRaw = entity.LinkRaw
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
