namespace SurveillanceCameras.Application.Repository;

public interface ICrawlStoryRepository
{
    Task FetchListStoryAsync(string webSourceId, string linkRaw, CancellationToken cancellationToken = default);
}
