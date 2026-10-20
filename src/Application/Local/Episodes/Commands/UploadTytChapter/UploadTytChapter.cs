using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Tyt;
using SurveillanceCameras.Application.Repository.Tyt;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Episodes.Commands.UploadTytChapter;

/// <summary>
/// Chạy nền: đăng một chương đã xử lý xong (tải + biên tập) lên TYT bằng tài khoản của truyện. Chương đã đăng
/// (<c>IsUploaded</c>) thì bỏ qua; chương đã có Id TYT mà nội dung vừa sửa thì dùng <paramref name="Force"/> để cập nhật.
/// Lỗi TYT được ghi vào <c>TytUploadError</c> thay vì ném ra (thử lại ngay thường vô ích: chưa đăng nhập, bị từ chối…);
/// tác vụ khôi phục lỗi sẽ thử lại khi phiên TYT dùng được.
/// </summary>
public record UploadTytChapterCommand(int ChapterId, bool Force = false) : IRequest;

public class UploadTytChapterCommandHandler : IRequestHandler<UploadTytChapterCommand>
{
    private const int MaxErrorLength = 2000;

    private readonly IApplicationDbContext _context;
    private readonly ITytRepository _tyt;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UploadTytChapterCommandHandler> _logger;

    public UploadTytChapterCommandHandler(IApplicationDbContext context, ITytRepository tyt, TimeProvider timeProvider,
        ILogger<UploadTytChapterCommandHandler> logger)
    {
        _context = context;
        _tyt = tyt;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Handle(UploadTytChapterCommand request, CancellationToken cancellationToken)
    {
        var chapter = await _context.Episodes
            .Include(x => x.Work)
            .FirstOrDefaultAsync(x => x.Id == request.ChapterId, cancellationToken);

        var story = chapter?.Work;
        if (chapter is null || story is null || story.Type != WorkType.Story) return;
        if (chapter.IsUploaded && !request.Force) return;
        // Chương chưa xử lý xong thì chưa có gì để đăng; lượt quét sau sẽ nhận lại khi xong.
        if (chapter.Status != EpisodeStatus.Edited || string.IsNullOrWhiteSpace(chapter.ContentEdit)) return;

        try
        {
            if (string.IsNullOrWhiteSpace(story.TytStoryId))
                throw new TytException("Truyện chưa có trên TYT — đưa truyện lên TYT trước khi đăng chương.");

            var context = await TytAccounts.ContextAsync(_context, story.AccountId, story.CreatedBy, cancellationToken);
            var content = new TytChapterRequest
            {
                Title = ChapterTitle(chapter.NameEdit ?? chapter.NameRaw, chapter.EpisodeNumber),
                Content = chapter.ContentEdit,
                Number = chapter.EpisodeNumber
            };

            if (string.IsNullOrWhiteSpace(chapter.TytChapterId))
            {
                chapter.TytChapterId = await _tyt.CreateChapterAsync(context, story.TytStoryId, content, cancellationToken);
                if (!chapter.IsUploaded) story.ChapterUploaded++;
            }
            else
            {
                await _tyt.UpdateChapterAsync(context, story.TytStoryId, chapter.TytChapterId, content, cancellationToken);
            }

            chapter.IsUploaded = true;
            chapter.TytUploadedAt = _timeProvider.GetUtcNow();
            chapter.TytUploadError = null;
            _logger.LogInformation("Chapter {ChapterId}: đã đăng lên TYT (truyện {TytStoryId})", chapter.Id, story.TytStoryId);
        }
        catch (TytException ex)
        {
            _logger.LogWarning("Chapter {ChapterId}: đăng lên TYT lỗi: {Error}", chapter.Id, ex.Message);
            chapter.TytUploadError = ex.Message.Length <= MaxErrorLength ? ex.Message : ex.Message[..MaxErrorLength];
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Tên chương: dùng tên đã có; thiếu thì "Chương {số}".</summary>
    public static string ChapterTitle(string? name, int number)
        => string.IsNullOrWhiteSpace(name) ? $"Chương {number}" : name.Trim();
}
