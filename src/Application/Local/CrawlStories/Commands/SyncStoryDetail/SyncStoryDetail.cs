using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Categories;
using SurveillanceCameras.Application.Local.StorySources.IntegrationEvents;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.CrawlStories.Commands.SyncStoryDetail;

/// <summary>
/// Lấy trang chi tiết (giới thiệu, thể loại…) của một nguồn truyện rồi ghi đè thông tin lấy từ danh sách. Chạy ở nền, từ
/// consumer Kafka — mỗi truyện một lần, nên các lần gọi tới trang nguồn được giãn cách. Thể loại trùng tên Category trong
/// hệ thống thì được gán vào nguồn truyện.
/// Nguồn truyện mới (<paramref name="IsNew"/>) có chi tiết xong mới phát sự kiện tạo truyện, để AI biên tập đúng giới thiệu.
/// Tải chi tiết lỗi thì vẫn dùng thông tin từ danh sách; nguồn đang chặn request thì ném
/// <see cref="CrawlBlockedException"/> để consumer chờ rồi làm lại.
/// </summary>
public record SyncStoryDetailCommand(int StorySourceId, bool IsNew) : IRequest;

public class SyncStoryDetailCommandHandler : IRequestHandler<SyncStoryDetailCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICrawlStoryRepositoryFactory _factory;
    private readonly ILogger<SyncStoryDetailCommandHandler> _logger;

    public SyncStoryDetailCommandHandler(IApplicationDbContext context, ICrawlStoryRepositoryFactory factory,
        ILogger<SyncStoryDetailCommandHandler> logger)
    {
        _context = context;
        _factory = factory;
        _logger = logger;
    }

    public async Task Handle(SyncStoryDetailCommand request, CancellationToken cancellationToken)
    {
        var source = await _context.StorySources
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.Id == request.StorySourceId, cancellationToken);

        if (source is null || string.IsNullOrWhiteSpace(source.LinkRaw)) return;

        var detail = await FetchDetailAsync(source, cancellationToken);

        if (detail is not null)
        {
            source.Title = detail.Title ?? source.Title;
            source.Author = detail.Author ?? source.Author;
            source.Description = detail.Description ?? source.Description;
            source.ImageUrl = detail.CoverUrl ?? source.ImageUrl;
            source.Status = detail.Status ?? source.Status;
            source.TotalChapters = detail.ChapterCount ?? source.TotalChapters;

            var categories = await CategoryMatcher.LoadAsync(_context, source.CreatedBy, cancellationToken);
            categories.AssignTo(source, detail.Genres);
        }

        // Nguồn mới đã có truyện (vd. xử lý lại message) thì không phát lại; tạo truyện vốn cũng bỏ qua nguồn đã có truyện.
        if (request.IsNew && !await _context.Works.AnyAsync(x => x.StorySourceId == source.Id, cancellationToken))
        {
            source.AddIntegrationEvent(new StorySourceCreatedIntegrationEvent
            {
                StorySourceId = source.Id,
                Title = source.Title,
                WebSourceId = source.WebSourceId,
                LinkRaw = source.LinkRaw
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Repository.CrawlStory.DTOs.StoryDataDto?> FetchDetailAsync(StorySource source, CancellationToken cancellationToken)
    {
        try
        {
            var crawl = await CrawlSource.LoadAsync(_context, source.CreatedBy, _factory, source.WebSourceId, cancellationToken);

            // Cấu hình không có phần "story" thì không có trang chi tiết để lấy: chỉ dùng thông tin danh sách.
            if (crawl.Context.Config is { Story: null }) return null;

            return await crawl.Repository.FetchStoryAsync(crawl.Context, source.LinkRaw!, cancellationToken);
        }
        catch (CrawlException ex) when (ex is not CrawlBlockedException)
        {
            _logger.LogWarning("StorySource {StorySourceId}: không lấy được trang chi tiết, dùng thông tin từ danh sách: {Error}",
                source.Id, ex.Message);
            return null;
        }
    }
}
