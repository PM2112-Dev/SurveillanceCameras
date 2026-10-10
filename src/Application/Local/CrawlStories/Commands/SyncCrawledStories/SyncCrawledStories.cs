using System.Collections.Concurrent;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Categories;
using SurveillanceCameras.Application.Local.StorySources.IntegrationEvents;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.DTOs;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.CrawlStories.Commands.SyncCrawledStories;

/// <summary>
/// Lưu các truyện đã chọn trong danh sách crawl vào <see cref="StorySource"/>. Truyện đã có (cùng nguồn web + link)
/// thì cập nhật thông tin, chưa có thì thêm mới — gọi lại nhiều lần không sinh bản trùng.
/// Mỗi truyện được tải thêm trang chi tiết để lấy giới thiệu (trang danh sách không có); tải lỗi thì vẫn lưu
/// thông tin từ danh sách. Thể loại trên trang chi tiết trùng tên Category trong hệ thống thì được gán vào nguồn truyện.
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

/// <param name="DetailFailed">Số truyện không tải được trang chi tiết (chỉ lưu thông tin từ danh sách).</param>
/// <param name="DetailError">Lỗi đầu tiên khi tải trang chi tiết, để biết nguyên nhân.</param>
public record SyncCrawledStoriesResult(int Created, int Updated, int DetailFailed, string? DetailError);

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
    // Số trang chi tiết tải song song: đủ nhanh mà không dồn dập vào trang nguồn.
    private const int DetailConcurrency = 5;

    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ICrawlStoryRepositoryFactory _factory;

    public SyncCrawledStoriesCommandHandler(IApplicationDbContext context, IUser user, ICrawlStoryRepositoryFactory factory)
    {
        _context = context;
        _user = user;
        _factory = factory;
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

        // Tải chi tiết trước khi ghi DB: lỗi giữa chừng (hoặc người dùng huỷ) thì không lưu dở dang.
        var (details, failed, firstError) = await FetchDetailsAsync(request.WebSourceId, stories, cancellationToken);

        var existing = await _context.StorySources
            .Where(x => x.CreatedBy == _user.Id && x.WebSourceId == request.WebSourceId && links.Contains(x.LinkRaw!))
            .Include(x => x.Categories)
            .ToListAsync(cancellationToken);
        var categories = await CategoryMatcher.LoadAsync(_context, _user.Id, cancellationToken);
        var byLink = existing.GroupBy(x => x.LinkRaw!).ToDictionary(g => g.Key, g => g.First());

        var newSources = new List<StorySource>();
        foreach (var story in stories)
        {
            if (!byLink.TryGetValue(story.Link, out var entity))
            {
                entity = new StorySource { WebSourceId = request.WebSourceId, LinkRaw = story.Link };
                _context.StorySources.Add(entity);
                newSources.Add(entity);
            }

            // Ưu tiên trang chi tiết (đầy đủ hơn), rồi tới trang danh sách; không có thì giữ dữ liệu đang lưu.
            details.TryGetValue(story.Link, out var detail);
            entity.Title = detail?.Title ?? story.Title ?? entity.Title;
            entity.Author = detail?.Author ?? story.Author ?? entity.Author;
            entity.Description = detail?.Description ?? entity.Description;
            entity.ImageUrl = detail?.CoverUrl ?? story.CoverUrl ?? entity.ImageUrl;
            entity.Status = detail?.Status ?? story.Status ?? entity.Status;
            entity.TotalChapters = detail?.ChapterCount ?? story.ChapterCount ?? entity.TotalChapters;
            categories.AssignTo(entity, detail?.Genres);
        }

        // Lưu lần một để có Id, rồi phát sự kiện cho từng nguồn mới: lần lưu thứ hai ghi OutboxMessage cùng transaction,
        // consumer sẽ dùng AI biên tập tên/giới thiệu và tạo Story (nguồn đã có trước đó không bị tạo lại).
        await _context.ExecuteInTransactionAsync(async ct =>
        {
            await _context.SaveChangesAsync(ct);

            foreach (var source in newSources)
            {
                source.AddIntegrationEvent(new StorySourceCreatedIntegrationEvent
                {
                    StorySourceId = source.Id,
                    Title = source.Title,
                    WebSourceId = source.WebSourceId,
                    LinkRaw = source.LinkRaw
                }, ct);
            }

            await _context.SaveChangesAsync(ct);
        }, cancellationToken);

        return new SyncCrawledStoriesResult(newSources.Count, stories.Count - newSources.Count, failed, firstError);
    }

    private async Task<(IReadOnlyDictionary<string, StoryDataDto> Details, int Failed, string? FirstError)> FetchDetailsAsync(
        int webSourceId, IReadOnlyList<SyncCrawledStoryItem> stories, CancellationToken cancellationToken)
    {
        var details = new ConcurrentDictionary<string, StoryDataDto>();
        var errors = new ConcurrentQueue<string>();

        CrawlSource source;
        try
        {
            source = await CrawlSource.LoadAsync(_context, _user.Id, _factory, webSourceId, cancellationToken);
        }
        catch (CrawlException ex)
        {
            return (details, stories.Count, ex.Message);
        }

        // Cấu hình không có phần "story" thì không có trang chi tiết để lấy: chỉ lưu thông tin danh sách.
        if (source.Context.Config is { Story: null })
            return (details, 0, null);

        await Parallel.ForEachAsync(stories,
            new ParallelOptions { MaxDegreeOfParallelism = DetailConcurrency, CancellationToken = cancellationToken },
            async (story, ct) =>
            {
                try
                {
                    details[story.Link] = await source.Repository.FetchStoryAsync(source.Context, story.Link, ct);
                }
                catch (CrawlException ex)
                {
                    errors.Enqueue($"{story.Title}: {ex.Message}");
                }
            });

        return (details, errors.Count, errors.TryPeek(out var first) ? first : null);
    }
}
