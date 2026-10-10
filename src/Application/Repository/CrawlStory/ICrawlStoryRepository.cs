using SurveillanceCameras.Application.Repository.CrawlStory.DTOs;

namespace SurveillanceCameras.Application.Repository.CrawlStory;

/// <summary>Lấy dữ liệu truyện trực tiếp từ trang nguồn (không lưu DB). Số trang bắt đầu từ 1.</summary>
public interface ICrawlStoryRepository
{
    Task<CrawlPageDto<StoryCrawlDto>> FetchStoryListAsync(CrawlContext context, int page,
        CancellationToken cancellationToken = default);

    Task<StoryDataDto> FetchStoryAsync(CrawlContext context, string storyUrl,
        CancellationToken cancellationToken = default);

    Task<CrawlPageDto<ChapterDataDto>> FetchChapterListAsync(CrawlContext context, string storyUrl, int page,
        CancellationToken cancellationToken = default);

    Task<ChapterContentDto> FetchChapterAsync(CrawlContext context, string chapterUrl,
        CancellationToken cancellationToken = default);
}
