using SurveillanceCameras.Application.CrawlData;
using StoryDataDto = SurveillanceCameras.Application.Service.WikiDichService.StoryDataDto;

namespace SurveillanceCameras.Application.WikiDichService;

public interface IWikiDichApiService
{
    Task<StoryDataDto> FetchStory(string linkRaw, CancellationToken cancellationToken = default);

    /// <summary>Crawl one page of a story listing (e.g. .../danh-sach/truyen-full/trang-{n}).</summary>
    Task<StoryListPageDto> FetchStoryList(string url, CancellationToken cancellationToken = default);
}
