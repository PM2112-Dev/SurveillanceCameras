using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.DTOs;

namespace SurveillanceCameras.Application.Local.CrawlStories.Queries.GetCrawledStory;

/// <summary>Lấy thông tin truyện trực tiếp từ trang nguồn (không lưu DB).</summary>
public record GetCrawledStoryQuery : IRequest<StoryDataDto>
{
    public int WebSourceId { get; init; }

    /// <summary>URL trang truyện trên trang nguồn (phải cùng host với nguồn web).</summary>
    public required string Url { get; init; }
}

public class GetCrawledStoryQueryValidator : AbstractValidator<GetCrawledStoryQuery>
{
    public GetCrawledStoryQueryValidator()
    {
        RuleFor(x => x.Url).NotEmpty();
    }
}

public class GetCrawledStoryQueryHandler : IRequestHandler<GetCrawledStoryQuery, StoryDataDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ICrawlStoryRepositoryFactory _factory;

    public GetCrawledStoryQueryHandler(IApplicationDbContext context, IUser user, ICrawlStoryRepositoryFactory factory)
    {
        _context = context;
        _user = user;
        _factory = factory;
    }

    public async Task<StoryDataDto> Handle(GetCrawledStoryQuery request, CancellationToken cancellationToken)
    {
        var source = await CrawlSource.LoadAsync(_context, _user.Id, _factory, request.WebSourceId, cancellationToken);

        if (!CrawlSource.IsSameHost(request.Url, source.Context.BaseUrl))
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.Url), "URL không thuộc nguồn web này.")]);

        return await source.Repository.FetchStoryAsync(source.Context, request.Url, cancellationToken);
    }
}
