using Microsoft.Extensions.DependencyInjection;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;
using SurveillanceCameras.Infrastructure.Repository.CrawlStory.WikiDich;

namespace SurveillanceCameras.Infrastructure.Repository.CrawlStory;

public class CrawlStoryRepositoryFactory : ICrawlStoryRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider;

    public CrawlStoryRepositoryFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ICrawlStoryRepository GetRepository(WebSource webSource)
    {
        if (!string.IsNullOrWhiteSpace(webSource.CrawlConfig))
            return _serviceProvider.GetRequiredService<ConfigurableCrawlStoryRepository>();

        // Repository viết bằng code: chỉ dành cho trang mà cấu hình crawl không diễn đạt được.
        return webSource.Type switch
        {
            WebSourceType.WIKIDICHZZZ => _serviceProvider.GetRequiredService<WikidichzzzRepository>(),
            WebSourceType.WIKIDICH => _serviceProvider.GetRequiredService<WikidichRepository>(),
            WebSourceType.WIKICV => _serviceProvider.GetRequiredService<WikicvRepository>(),
            _ => throw new CrawlException("Nguồn web chưa có cấu hình crawl.")
        };
    }
}
