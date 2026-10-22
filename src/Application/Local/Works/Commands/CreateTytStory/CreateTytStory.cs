using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Local.Tyt;
using SurveillanceCameras.Application.Local.WebSources;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Application.Repository.Tyt;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Works.Commands.CreateTytStory;

/// <summary>
/// Chạy nền: đưa truyện lên TYT bằng tài khoản của truyện — chưa có trên TYT thì tạo và lưu Id vào
/// <see cref="Work.TytStoryId"/>, sau đó cập nhật thông tin (tên, tác giả, giới thiệu, trạng thái, loại truyện, ảnh bìa
/// và thể loại lấy từ nguồn truyện).
/// Chạy lại an toàn: truyện đã có Id thì chỉ cập nhật thông tin. Lỗi được ghi vào <see cref="Work.TytError"/> thay vì
/// ném ra — lỗi TYT thường là chưa đăng nhập/bị từ chối, thử lại tự động vô ích; người dùng bấm lại sau khi xử lý.
/// </summary>
public record CreateTytStoryCommand(int WorkId) : IRequest;

public class CreateTytStoryCommandHandler : IRequestHandler<CreateTytStoryCommand>
{
    private const int MaxErrorLength = 2000;

    private readonly IApplicationDbContext _context;
    private readonly ITytRepository _tyt;
    private readonly TimeProvider _timeProvider;
    private readonly TytPolicyOptions _policy;
    private readonly ILogger<CreateTytStoryCommandHandler> _logger;

    public CreateTytStoryCommandHandler(IApplicationDbContext context, ITytRepository tyt, TimeProvider timeProvider,
        IOptions<TytPolicyOptions> policy, ILogger<CreateTytStoryCommandHandler> logger)
    {
        _context = context;
        _tyt = tyt;
        _timeProvider = timeProvider;
        _policy = policy.Value;
        _logger = logger;
    }

    public async Task Handle(CreateTytStoryCommand request, CancellationToken cancellationToken)
    {
        var story = await _context.Works.FirstOrDefaultAsync(x => x.Id == request.WorkId, cancellationToken);
        if (story is null) return;

        try
        {
            var context = await TytContextAsync(story, cancellationToken);

            if (string.IsNullOrWhiteSpace(story.TytStoryId))
            {
                // Mỗi tài khoản chỉ đăng cùng lúc tối đa N truyện chưa hoàn thành: đủ rồi thì chờ có chỗ trống (không tính là lỗi).
                var active = await TytSlots.ActiveCountAsync(_context, context.AccountId, story.Id, cancellationToken);
                if (active >= _policy.MaxActiveStoriesPerAccount)
                {
                    story.TytStage = TytStage.WaitingSlot;
                    story.TytStageNote = $"Tài khoản TYT đã có {active}/{_policy.MaxActiveStoriesPerAccount} truyện đang đăng — truyện sẽ được tạo khi có chỗ trống (một truyện hoàn thành).";
                    story.TytError = null;
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Story {StoryId}: tài khoản TYT {AccountId} đủ {Active} truyện, chờ chỗ trống", story.Id, context.AccountId, active);
                    return;
                }


                story.TytStoryId = await FindOrCreateAsync(context, story, cancellationToken);
                // Lưu Id ngay: bước cập nhật thông tin có lỗi thì lần chạy lại không tạo trùng truyện trên TYT.
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Story {StoryId}: đã tạo trên TYT với Id {TytStoryId}", story.Id, story.TytStoryId);
            }

            story.TytError = await UpdateInfoAsync(context, story, cancellationToken);
            story.TytUpdatedAt = _timeProvider.GetUtcNow();
        }
        catch (TytException ex)
        {
            _logger.LogWarning("Story {StoryId}: đưa lên TYT lỗi: {Error}", story.Id, ex.Message);
            var message = string.IsNullOrWhiteSpace(story.TytStoryId)
                ? ex.Message
                : $"Đã có truyện trên TYT nhưng cập nhật thông tin lỗi: {ex.Message}";
            story.TytError = message.Length <= MaxErrorLength ? message : message[..MaxErrorLength];
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<TytContext> TytContextAsync(Work story, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(story.Title) || story.Title.Trim().Length < 3)
            throw new TytException("Truyện chưa có tên (tối thiểu 3 ký tự) để đưa lên TYT.");

        var account = story.AccountId is { } accountId
            ? await _context.Accounts
                .AsNoTracking()
                .Include(x => x.AccountType)
                .FirstOrDefaultAsync(x => x.Id == accountId && x.CreatedBy == story.CreatedBy, cancellationToken)
            : null;

        if (account is null)
            throw new TytException("Truyện chưa chọn tài khoản TYT đăng truyện.");
        if (string.IsNullOrWhiteSpace(account.AccountType?.BaseUrl))
            throw new TytException($"Loại tài khoản “{account.AccountType?.Title}” chưa có Base URL (vd. https://tytnovel.info/).");

        return new TytContext(account.Id, account.AccountType.BaseUrl, account.AccountType.LoginCookieName);
    }

    private async Task<string> FindOrCreateAsync(TytContext context, Work story, CancellationToken cancellationToken)
    {
        // Lần chạy trước có thể đã tạo xong trên TYT nhưng chưa kịp lưu Id (mất kết nối, app dừng giữa chừng): dùng lại
        // truyện cùng tên chưa gắn với truyện nào trong hệ thống thay vì tạo trùng.
        var existingId = await _tyt.GetStoryIdAsync(context, story.Title!, cancellationToken);
        if (existingId is not null
            && !await _context.Works.AnyAsync(x => x.TytStoryId == existingId && x.Id != story.Id, cancellationToken))
        {
            _logger.LogInformation("Story {StoryId}: dùng lại truyện cùng tên đã có trên TYT ({TytStoryId})", story.Id, existingId);
            return existingId;
        }

        return await _tyt.CreateStoryAsync(context, new TytCreateStoryRequest
        {
            Title = story.Title!.Trim(),
            Author = story.Author
        }, cancellationToken);
    }

    /// <summary>
    /// Cập nhật thông tin lên TYT. TYT có thể từ chối ảnh bìa (vd. ảnh có chữ Trung) hoặc thể loại không có trong danh
    /// sách của TYT: khi đó gửi lại không kèm hai trường này để giới thiệu, trạng thái… vẫn được cập nhật.
    /// </summary>
    /// <returns>Cảnh báo nếu phải bỏ ảnh bìa/thể loại; <c>null</c> nếu cập nhật đầy đủ.</returns>
    private async Task<string?> UpdateInfoAsync(TytContext context, Work story, CancellationToken cancellationToken)
    {
        var source = await _context.StorySources
            .AsNoTracking()
            .Where(x => x.Id == story.StorySourceId)
            .Select(x => new { x.WebSourceId, x.ImageUrl, Categories = x.Categories.Select(c => c.Title).ToList() })
            .FirstOrDefaultAsync(cancellationToken);
        var isNeedEdit = await WebSourceEditPolicy.IsNeedEditAsync(_context, source?.WebSourceId, cancellationToken);

        var info = BaseRequest(story, isNeedEdit);
        var cover = string.IsNullOrWhiteSpace(source?.ImageUrl) ? null : source.ImageUrl;
        var genres = source?.Categories.OfType<string>().Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [];

        if (cover is null && genres.Count == 0)
        {
            await _tyt.UpdateStoryAsync(context, story.TytStoryId!, info, cancellationToken);
            return null;
        }

        try
        {
            // Không có thể loại khớp thì giữ thể loại đang có trên TYT (không xoá trắng).
            await _tyt.UpdateStoryAsync(context, story.TytStoryId!, info with
            {
                CoverUrl = cover,
                Genres = genres.Count > 0 ? genres : null
            }, cancellationToken);
            return null;
        }
        catch (TytException ex)
        {
            await _tyt.UpdateStoryAsync(context, story.TytStoryId!, info, cancellationToken);
            _logger.LogWarning("Story {StoryId}: TYT không nhận ảnh bìa/thể loại: {Error}", story.Id, ex.Message);
            return $"Đã cập nhật thông tin nhưng TYT không nhận ảnh bìa/thể loại: {ex.Message}";
        }
    }

    private static TytUpdateStoryRequest BaseRequest(Work story, bool isNeedEdit)
        => new()
        {
            Title = story.Title!.Trim(),
            Author = string.IsNullOrWhiteSpace(story.Author) ? null : story.Author.Trim(),
            // Chưa có giới thiệu thì giữ nội dung đang có trên TYT (không xoá trắng).
            Description = string.IsNullOrWhiteSpace(story.Description) ? null : story.Description,
            Status = story.IsFull ? TytStoryStatus.Full : TytStoryStatus.Ongoing,
            // Nguồn được AI biên tập lại → "Dịch / Edit"; giữ nguyên bản convert của nguồn → "Convert / QT".
            Type = isNeedEdit ? TytStoryType.Translated : TytStoryType.Convert
        };
}
