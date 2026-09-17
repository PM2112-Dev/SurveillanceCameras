using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.CrawlStories.Queries.FetchListStory;

public record FetchListStoryQuery : IRequest
{
    public string? WebSourceId { get; init; }
    public string? LinkRaw { get; init; }
}

public class FetchListStoryQueryValidator : AbstractValidator<FetchListStoryQuery>
{
    public FetchListStoryQueryValidator()
    {
    }
}

public class FetchListStoryQueryHandler : IRequestHandler<FetchListStoryQuery>
{
    private readonly IApplicationDbContext _context;

    public FetchListStoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(FetchListStoryQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
