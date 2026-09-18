using Microsoft.Extensions.DependencyInjection;
using SurveillanceCameras.Application.Repository;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Infrastructure.Repository;

public class CrawlStoryRepositoryFactory : ICrawlStoryRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider;

    public CrawlStoryRepositoryFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ICrawlStoryRepository GetRepository(WebSourceType type)
    {
        return type switch
        {
            WebSourceType.WIKIDICHZZZ => _serviceProvider.GetRequiredService<WikidichzzzRepository>(),
            WebSourceType.WIKIDICH => _serviceProvider.GetRequiredService<WikidichRepository>(),
            WebSourceType.WIKICV => _serviceProvider.GetRequiredService<WikicvRepository>(),
            _ => throw new NotSupportedException($"WebSourceType '{type}' is not supported.")
        };
    }
}
