using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Episodes.Commands.RequestTytChapterUploads;

/// <summary>
/// Người dùng bấm đăng chương lên TYT (không phụ thuộc lịch): đưa <paramref name="Count"/> chương xử lý xong kế tiếp vào
/// hàng đợi, hoặc tất cả khi không nêu số. Chương đã lỗi đăng được xoá lỗi để đăng lại.
/// </summary>
/// <returns>Số chương được đưa vào hàng đợi.</returns>
public record RequestTytChapterUploadsCommand(int WorkId, int? Count = null) : IRequest<int>;

public class RequestTytChapterUploadsCommandHandler : IRequestHandler<RequestTytChapterUploadsCommand, int>
{
    private const int MaxPerRequest = 500;

    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public RequestTytChapterUploadsCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<int> Handle(RequestTytChapterUploadsCommand request, CancellationToken cancellationToken)
    {
        var story = await _context.Works
            .FirstOrDefaultAsync(x => x.Id == request.WorkId && x.CreatedBy == _user.Id && x.Type == WorkType.Story, cancellationToken);
        Guard.Against.NotFound(request.WorkId, story);

        if (!TytUploadQueue.CanUpload(story))
        {
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.WorkId), "Truyện chưa có trên TYT (hoặc chưa chọn tài khoản TYT) — đưa truyện lên TYT trước.")]);
        }

        // Người dùng chủ động đăng lại: bỏ lỗi cũ để chương lỗi không chặn hàng đợi.
        var failed = await _context.Episodes
            .Where(x => x.WorkId == story.Id && !x.IsUploaded && x.TytUploadError != null)
            .ToListAsync(cancellationToken);
        foreach (var chapter in failed) { chapter.TytUploadError = null; chapter.QueuedAt = null; }
        if (failed.Count > 0) await _context.SaveChangesAsync(cancellationToken);

        var take = Math.Clamp(request.Count ?? MaxPerRequest, 1, MaxPerRequest);
        var queued = await TytUploadQueue.QueueNextAsync(_context, story, take, _timeProvider.GetUtcNow(), cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return queued;
    }
}
