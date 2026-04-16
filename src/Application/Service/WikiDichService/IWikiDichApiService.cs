using SurveillanceCameras.Application.Local.Stories.Queries.Model;

namespace SurveillanceCameras.Application.Service.WikiDichService;

public interface IWikiDichApiService
{
    Task<StoryDataDto> FetchStory(string linkRaw, CancellationToken cancellationToken = default);
}
