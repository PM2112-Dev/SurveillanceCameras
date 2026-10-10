using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Works.IntegrationEvents;

namespace SurveillanceCameras.Application.Local.Works.Commands.RequestTytStoryCreation;

/// <summary>
/// Yêu cầu đưa truyện lên TYT (chạy nền qua Kafka): chưa có thì tạo, đã có thì cập nhật lại thông tin. Dùng cho truyện
/// tạo trước khi nguồn web có tài khoản TYT, chạy lại sau khi lỗi (chưa đăng nhập TYT…) hoặc sau khi sửa truyện.
/// </summary>
public record RequestTytStoryCreationCommand(int WorkId) : IRequest;

public class RequestTytStoryCreationCommandHandler : IRequestHandler<RequestTytStoryCreationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public RequestTytStoryCreationCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(RequestTytStoryCreationCommand request, CancellationToken cancellationToken)
    {
        var story = await _context.Works
            .FirstOrDefaultAsync(x => x.Id == request.WorkId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.WorkId, story);

        if (story.AccountId is null)
        {
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.WorkId), "Truyện chưa chọn tài khoản TYT đăng truyện — sửa truyện và chọn tài khoản trước.")]);
        }

        story.TytError = null;
        story.AddIntegrationEvent(new TytStoryCreateRequestedIntegrationEvent { StoryId = story.Id }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
