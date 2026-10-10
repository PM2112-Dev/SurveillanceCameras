using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.DTOs;

namespace SurveillanceCameras.Application.Local.CrawlStories.Queries.GetCrawledChapter;

/// <summary>Lấy nội dung một chương trực tiếp từ trang nguồn (không lưu DB).</summary>
public record GetCrawledChapterQuery : IRequest<ChapterContentDto>
{
    public int WebSourceId { get; init; }

    /// <summary>URL trang chương trên trang nguồn (phải cùng host với nguồn web).</summary>
    public required string Url { get; init; }
}

public class GetCrawledChapterQueryValidator : AbstractValidator<GetCrawledChapterQuery>
{
    public GetCrawledChapterQueryValidator()
    {
        RuleFor(x => x.Url).NotEmpty();
    }
}

public class GetCrawledChapterQueryHandler : IRequestHandler<GetCrawledChapterQuery, ChapterContentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ICrawlStoryRepositoryFactory _factory;

    public GetCrawledChapterQueryHandler(IApplicationDbContext context, IUser user, ICrawlStoryRepositoryFactory factory)
    {
        _context = context;
        _user = user;
        _factory = factory;
    }

    public async Task<ChapterContentDto> Handle(GetCrawledChapterQuery request, CancellationToken cancellationToken)
    {
        var source = await CrawlSource.LoadAsync(_context, _user.Id, _factory, request.WebSourceId, cancellationToken);

        if (!CrawlSource.IsSameHost(request.Url, source.Context.BaseUrl))
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.Url), "URL không thuộc nguồn web này.")]);

        return await source.Repository.FetchChapterAsync(source.Context, request.Url, cancellationToken);
    }
}
