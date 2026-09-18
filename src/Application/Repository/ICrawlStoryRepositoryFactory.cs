using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Repository;

public interface ICrawlStoryRepositoryFactory
{
    ICrawlStoryRepository GetRepository(WebSourceType type);
}
