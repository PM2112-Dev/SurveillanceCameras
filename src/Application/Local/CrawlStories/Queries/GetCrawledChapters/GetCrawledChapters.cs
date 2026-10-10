using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.DTOs;

namespace SurveillanceCameras.Application.Local.CrawlStories.Queries.GetCrawledChapters;

/// <summary>Lấy một trang mục lục chương của truyện trực tiếp từ trang nguồn (không lưu DB).</summary>
public record GetCrawledChaptersQuery : IRequest<CrawlPageDto<ChapterDataDto>>
{
    public int WebSourceId { get; init; }

    /// <summary>URL trang truyện trên trang nguồn (phải cùng host với nguồn web).</summary>
    public required string Url { get; init; }

    public int Page { get; init; } = 1;
}

public class GetCrawledChaptersQueryValidator : AbstractValidator<GetCrawledChaptersQuery>
{
    public GetCrawledChaptersQueryValidator()
    {
        RuleFor(x => x.Url).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
    }
}

public class GetCrawledChaptersQueryHandler : IRequestHandler<GetCrawledChaptersQuery, CrawlPageDto<ChapterDataDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ICrawlStoryRepositoryFactory _factory;

    public GetCrawledChaptersQueryHandler(IApplicationDbContext context, IUser user, ICrawlStoryRepositoryFactory factory)
    {
        _context = context;
        _user = user;
        _factory = factory;
    }

    public async Task<CrawlPageDto<ChapterDataDto>> Handle(GetCrawledChaptersQuery request, CancellationToken cancellationToken)
    {
        var source = await CrawlSource.LoadAsync(_context, _user.Id, _factory, request.WebSourceId, cancellationToken);

        if (!CrawlSource.IsSameHost(request.Url, source.Context.BaseUrl))
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.Url), "URL không thuộc nguồn web này.")]);

        return await source.Repository.FetchChapterListAsync(source.Context, request.Url, request.Page, cancellationToken);
    }
}
