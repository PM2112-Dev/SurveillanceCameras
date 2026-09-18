using SurveillanceCameras.Application.Local.CrawlStories.Queries.FetchListStory;

namespace SurveillanceCameras.Application.Crawl;

public interface IWikidichzzzApiService
{
    Task FetchListStoryAsync(string webSourceId, string linkRaw, CancellationToken cancellationToken = default);
}
