using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Categories;
using SurveillanceCameras.Application.Local.CrawlStories;
using SurveillanceCameras.Application.Repository.CrawlStory;

namespace SurveillanceCameras.Application.Local.StorySources.Commands.RefreshStorySource;

/// <summary>
/// Lấy lại thông tin truyện từ trang nguồn (<c>LinkRaw</c>) và cập nhật vào StorySource — bổ sung mô tả, ảnh bìa…
/// mà lúc đồng bộ từ trang danh sách không có. Trường nào trang nguồn không trả về thì giữ giá trị đang lưu.
/// Thể loại trùng tên Category trong hệ thống thì được gán thêm.
/// </summary>
public record RefreshStorySourceCommand(int Id) : IRequest;

public class RefreshStorySourceCommandValidator : AbstractValidator<RefreshStorySourceCommand>
{
    public RefreshStorySourceCommandValidator()
    {
    }
}

public class RefreshStorySourceCommandHandler : IRequestHandler<RefreshStorySourceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ICrawlStoryRepositoryFactory _factory;

    public RefreshStorySourceCommandHandler(IApplicationDbContext context, IUser user, ICrawlStoryRepositoryFactory factory)
    {
        _context = context;
        _user = user;
        _factory = factory;
    }

    public async Task Handle(RefreshStorySourceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StorySources
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        if (string.IsNullOrWhiteSpace(entity.LinkRaw))
            throw new CrawlException("Nguồn truyện chưa có link trang gốc.");

        var source = await CrawlSource.LoadAsync(_context, _user.Id, _factory, entity.WebSourceId, cancellationToken);

        if (!CrawlSource.IsSameHost(entity.LinkRaw, source.Context.BaseUrl))
            throw new CrawlException("Link trang gốc không thuộc nguồn web của truyện.");

        var story = await source.Repository.FetchStoryAsync(source.Context, entity.LinkRaw, cancellationToken);

        entity.Title = story.Title ?? entity.Title;
        entity.Author = story.Author ?? entity.Author;
        entity.Description = story.Description ?? entity.Description;
        entity.ImageUrl = story.CoverUrl ?? entity.ImageUrl;
        entity.Status = story.Status ?? entity.Status;
        entity.TotalChapters = story.ChapterCount ?? entity.TotalChapters;

        var categories = await CategoryMatcher.LoadAsync(_context, _user.Id, cancellationToken);
        categories.AssignTo(entity, story.Genres);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
