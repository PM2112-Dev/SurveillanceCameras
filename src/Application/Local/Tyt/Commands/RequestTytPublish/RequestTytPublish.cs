using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Tyt.Commands.RequestTytPublish;

/// <summary>
/// Xuất bản truyện lên TYT: đăng vài chương đầu rồi gửi yêu cầu công khai và chờ TYT duyệt (làm ở nền, xem
/// <c>AdvanceTytPublishCommand</c>). Sau khi được duyệt mới đăng các chương còn lại theo lịch của truyện.
/// </summary>
public record RequestTytPublishCommand(int WorkId) : IRequest;

public class RequestTytPublishCommandHandler : IRequestHandler<RequestTytPublishCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public RequestTytPublishCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(RequestTytPublishCommand request, CancellationToken cancellationToken)
    {
        var story = await _context.Works
            .FirstOrDefaultAsync(x => x.Id == request.WorkId && x.CreatedBy == _user.Id && x.Type == WorkType.Story, cancellationToken);
        Guard.Against.NotFound(request.WorkId, story);

        void Refuse(string message) => throw new Common.Exceptions.ValidationException([
            new FluentValidation.Results.ValidationFailure(nameof(request.WorkId), message)]);

        if (story.AccountId is null) Refuse("Truyện chưa chọn tài khoản TYT đăng truyện.");
        if (string.IsNullOrWhiteSpace(story.TytStoryId)) Refuse("Truyện chưa có trên TYT — bấm “Tạo trên TYT” trước (nếu tài khoản đã đủ số truyện đang đăng, truyện sẽ chờ có chỗ trống).");
        if (story.TytStage is TytStage.PendingApproval or TytStage.Approved) Refuse("Truyện đã gửi yêu cầu công khai hoặc đã được TYT duyệt.");

        story.TytStage = TytStage.PublishRequested;
        story.TytStageNote = "Đang chuẩn bị các chương đầu để gửi yêu cầu công khai.";
        story.TytError = null;
        story.TytRetryQueuedAt = null;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
