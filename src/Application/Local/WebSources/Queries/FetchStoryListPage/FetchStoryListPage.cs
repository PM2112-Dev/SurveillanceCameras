using SurveillanceCameras.Application.CrawlData;
using SurveillanceCameras.Application.WikiDichService;

namespace SurveillanceCameras.Application.Local.WebSources.Queries.FetchStoryListPage;

/// <summary>
/// Reads one page of a story listing from an external source (WikiDich) — a "Query" in the
/// CQRS sense even though it never touches our own DB: it doesn't mutate our system state,
/// it just returns data crawled from outside. Called by FetchStoryWebSourceConsumer
/// (Infrastructure), never from a controller directly.
/// </summary>
public record FetchStoryListPageQuery : IRequest<StoryListPageDto>
{
    public required string PageUrl { get; init; }
}

public class FetchStoryListPageQueryValidator : AbstractValidator<FetchStoryListPageQuery>
{
    public FetchStoryListPageQueryValidator()
    {
        RuleFor(x => x.PageUrl).NotEmpty();
    }
}

public class FetchStoryListPageQueryHandler : IRequestHandler<FetchStoryListPageQuery, StoryListPageDto>
{
    private readonly IWikiDichApiService _wikiDichApiService;

    public FetchStoryListPageQueryHandler(IWikiDichApiService wikiDichApiService)
    {
        _wikiDichApiService = wikiDichApiService;
    }

    public Task<StoryListPageDto> Handle(FetchStoryListPageQuery request, CancellationToken cancellationToken)
        => _wikiDichApiService.FetchStoryList(request.PageUrl, cancellationToken);
}
