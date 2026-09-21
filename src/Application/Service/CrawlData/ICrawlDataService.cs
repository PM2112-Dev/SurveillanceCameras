namespace SurveillanceCameras.Application.CrawlData;

public interface ICrawlDataService
{
    Task<StoryListPageDto> FetchStory(string baseUrl, int webSourceId, CancellationToken cancellationToken = default);
    
    Task<StoryDataDto> FetchStoryData(string linkUrl, int webSourceId, CancellationToken cancellationToken = default);
    
    Task<ChapterDataDto> FetchChapterData(string linkUrl, int webSourceId, CancellationToken cancellationToken = default);
}
