using SurveillanceCameras.Application.Service.WikiDichService;

namespace SurveillanceCameras.Application.WikiDichService;

public interface IWikiDichApiService
{
    Task<StoryDataDto> FetchStory(string linkRaw, CancellationToken cancellationToken = default);
}
