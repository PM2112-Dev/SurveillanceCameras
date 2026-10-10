using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.DTOs;

namespace SurveillanceCameras.Infrastructure.Repository.CrawlStory.WikiDich;

/// <summary>
/// Chỗ để viết cách crawl Wikidichzzz bằng code nếu cấu hình crawl không đủ. Hiện chưa dùng: nguồn có
/// <c>CrawlConfig</c> luôn đi qua ConfigurableCrawlStoryRepository.
/// </summary>
public class WikidichzzzRepository : ICrawlStoryRepository
{
    private static CrawlException NotImplemented()
        => new("Chưa có cách crawl Wikidichzzz viết bằng code — hãy khai báo cấu hình crawl cho nguồn web.");

    public Task<CrawlPageDto<StoryCrawlDto>> FetchStoryListAsync(CrawlContext context, int page,
        CancellationToken cancellationToken = default) => throw NotImplemented();

    public Task<StoryDataDto> FetchStoryAsync(CrawlContext context, string storyUrl,
        CancellationToken cancellationToken = default) => throw NotImplemented();

    public Task<CrawlPageDto<ChapterDataDto>> FetchChapterListAsync(CrawlContext context, string storyUrl, int page,
        CancellationToken cancellationToken = default) => throw NotImplemented();

    public Task<ChapterContentDto> FetchChapterAsync(CrawlContext context, string chapterUrl,
        CancellationToken cancellationToken = default) => throw NotImplemented();
}
