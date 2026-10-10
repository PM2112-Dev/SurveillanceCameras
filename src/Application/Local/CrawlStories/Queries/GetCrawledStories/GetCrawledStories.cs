using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.DTOs;

namespace SurveillanceCameras.Application.Local.CrawlStories.Queries.GetCrawledStories;

/// <summary>Lấy một trang danh sách truyện trực tiếp từ trang nguồn (không lưu DB).</summary>
public record GetCrawledStoriesQuery : IRequest<CrawlPageDto<StoryCrawlDto>>
{
    public int WebSourceId { get; init; }

    public int Page { get; init; } = 1;
}

public class GetCrawledStoriesQueryValidator : AbstractValidator<GetCrawledStoriesQuery>
{
    public GetCrawledStoriesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
    }
}

public class GetCrawledStoriesQueryHandler : IRequestHandler<GetCrawledStoriesQuery, CrawlPageDto<StoryCrawlDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ICrawlStoryRepositoryFactory _factory;

    public GetCrawledStoriesQueryHandler(IApplicationDbContext context, IUser user, ICrawlStoryRepositoryFactory factory)
    {
        _context = context;
        _user = user;
        _factory = factory;
    }

    public async Task<CrawlPageDto<StoryCrawlDto>> Handle(GetCrawledStoriesQuery request, CancellationToken cancellationToken)
    {
        var source = await CrawlSource.LoadAsync(_context, _user.Id, _factory, request.WebSourceId, cancellationToken);

        var page = await source.Repository.FetchStoryListAsync(source.Context, request.Page, cancellationToken);

        var links = page.Items.Select(s => s.Link).OfType<string>().ToList();
        var synced = await _context.StorySources
            .Where(x => x.CreatedBy == _user.Id && x.WebSourceId == request.WebSourceId && links.Contains(x.LinkRaw!))
            .Select(x => x.LinkRaw!)
            .ToListAsync(cancellationToken);
        var syncedSet = synced.ToHashSet();

        foreach (var story in page.Items) story.IsSynced = story.Link is not null && syncedSet.Contains(story.Link);

        return page;
    }
}
