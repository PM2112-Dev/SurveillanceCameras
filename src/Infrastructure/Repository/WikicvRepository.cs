using SurveillanceCameras.Application.Repository;

namespace SurveillanceCameras.Infrastructure.Repository;

public class WikicvRepository : ICrawlStoryRepository
{
    public Task FetchListStoryAsync(string webSourceId, string linkRaw, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
