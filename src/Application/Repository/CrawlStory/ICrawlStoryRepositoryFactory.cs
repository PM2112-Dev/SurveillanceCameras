using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Repository.CrawlStory;

public interface ICrawlStoryRepositoryFactory
{
    /// <summary>
    /// Nguồn có <c>CrawlConfig</c> thì dùng repository chạy theo cấu hình; không có thì dùng repository
    /// viết bằng code theo <c>WebSource.Type</c> (cho trang cấu hình không diễn đạt được).
    /// </summary>
    ICrawlStoryRepository GetRepository(WebSource webSource);
}
