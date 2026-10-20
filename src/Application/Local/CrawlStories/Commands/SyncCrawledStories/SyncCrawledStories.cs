using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.CrawlStories.IntegrationEvents;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.CrawlStories.Commands.SyncCrawledStories;

/// <summary>
/// Lưu các truyện đã chọn trong danh sách crawl vào <see cref="StorySource"/>. Truyện đã có (cùng nguồn web + link)
/// thì cập nhật thông tin, chưa có thì thêm mới — gọi lại nhiều lần không sinh bản trùng.
/// Chỉ lưu thông tin từ danh sách rồi xếp từng truyện vào Kafka (<see cref="StoryDetailSyncRequestedIntegrationEvent"/>):
/// trang chi tiết (giới thiệu, thể loại) được consumer lấy lần lượt, mỗi lần cách nhau theo giãn cách của nguồn web, nên
/// không gọi dồn dập vào trang nguồn và yêu cầu trả về ngay (xem <see cref="SyncStoryDetail.SyncStoryDetailCommand"/>).
/// </summary>
public record SyncCrawledStoriesCommand : IRequest<SyncCrawledStoriesResult>
{
    public const int MaxStories = 500;

    public int WebSourceId { get; init; }

    public List<SyncCrawledStoryItem> Stories { get; init; } = [];
}

public record SyncCrawledStoryItem
{
    public required string Link { get; init; }

    public string? Title { get; init; }

    public string? Author { get; init; }

    public string? CoverUrl { get; init; }

    public string? Status { get; init; }

    public int? ChapterCount { get; init; }
}

/// <param name="Queued">Số truyện đã xếp hàng chờ lấy trang chi tiết (xử lý ở nền, lần lượt).</param>
public record SyncCrawledStoriesResult(int Created, int Updated, int Queued);

public class SyncCrawledStoriesCommandValidator : AbstractValidator<SyncCrawledStoriesCommand>
{
    public SyncCrawledStoriesCommandValidator()
    {
        RuleFor(x => x.Stories)
            .NotEmpty().WithMessage("Chưa chọn truyện nào.")
            .Must(stories => stories.Count <= SyncCrawledStoriesCommand.MaxStories)
            .WithMessage($"Mỗi lần đồng bộ tối đa {SyncCrawledStoriesCommand.MaxStories} truyện.");

        RuleForEach(x => x.Stories).ChildRules(story =>
        {
            story.RuleFor(x => x.Link).NotEmpty().MaximumLength(2000);
            story.RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        });
    }
}

public class SyncCrawledStoriesCommandHandler : IRequestHandler<SyncCrawledStoriesCommand, SyncCrawledStoriesResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SyncCrawledStoriesCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<SyncCrawledStoriesResult> Handle(SyncCrawledStoriesCommand request, CancellationToken cancellationToken)
    {
        var webSource = await _context.WebSources
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.WebSourceId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.WebSourceId, webSource);

        // Link phải thuộc đúng trang nguồn: không cho lưu link tuỳ ý vào nguồn web này.
        var invalid = request.Stories.FirstOrDefault(s => !CrawlSource.IsSameHost(s.Link, webSource.BaseUrl ?? ""));
        if (invalid is not null)
        {
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.Stories), $"Link “{invalid.Link}” không thuộc nguồn web này.")]);
        }

        var stories = request.Stories.DistinctBy(s => s.Link).ToList();
        var links = stories.Select(s => s.Link).ToList();

        var existing = await _context.StorySources
            .Where(x => x.CreatedBy == _user.Id && x.WebSourceId == request.WebSourceId && links.Contains(x.LinkRaw!))
            .ToListAsync(cancellationToken);
        var byLink = existing.GroupBy(x => x.LinkRaw!).ToDictionary(g => g.Key, g => g.First());

        var newSources = new List<StorySource>();
        var all = new List<StorySource>();
        foreach (var story in stories)
        {
            if (!byLink.TryGetValue(story.Link, out var entity))
            {
                entity = new StorySource { WebSourceId = request.WebSourceId, LinkRaw = story.Link };
                _context.StorySources.Add(entity);
                newSources.Add(entity);
            }

            // Thông tin từ danh sách; trang chi tiết (đầy đủ hơn) được consumer lấy sau và ghi đè.
            entity.Title = story.Title ?? entity.Title;
            entity.Author = story.Author ?? entity.Author;
            entity.ImageUrl = story.CoverUrl ?? entity.ImageUrl;
            entity.Status = story.Status ?? entity.Status;
            entity.TotalChapters = story.ChapterCount ?? entity.TotalChapters;
            all.Add(entity);
        }

        // Lưu lần một để nguồn mới có Id, rồi phát sự kiện cho từng truyện: lần lưu thứ hai ghi OutboxMessage cùng transaction,
        // nên đã lưu truyện thì chắc chắn có yêu cầu lấy chi tiết đi kèm.
        await _context.ExecuteInTransactionAsync(async ct =>
        {
            await _context.SaveChangesAsync(ct);

            foreach (var source in all)
            {
                source.AddIntegrationEvent(new StoryDetailSyncRequestedIntegrationEvent
                {
                    StorySourceId = source.Id,
                    IsNew = newSources.Contains(source)
                }, ct);
            }

            await _context.SaveChangesAsync(ct);
        }, cancellationToken);

        return new SyncCrawledStoriesResult(newSources.Count, stories.Count - newSources.Count, all.Count);
    }
}
