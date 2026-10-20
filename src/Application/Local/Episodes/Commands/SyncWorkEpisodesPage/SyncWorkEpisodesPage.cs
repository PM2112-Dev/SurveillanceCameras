using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;
using SurveillanceCameras.Application.Local.CrawlStories;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Episodes.Commands.SyncWorkEpisodesPage;

/// <summary>
/// Chạy nền: đồng bộ MỘT trang mục lục chương của truyện từ trang nguồn. Chương chưa có (theo link) được thêm với
/// số chương nối tiếp và phát sự kiện tải nội dung; còn trang sau thì phát sự kiện cho trang sau. Chương đã có được giữ
/// nguyên, nên chạy lại (đồng bộ chương mới của truyện đang ra) chỉ thêm phần mới.
/// </summary>
public record SyncWorkEpisodesPageCommand(int WorkId, int Page) : IRequest;

public class SyncWorkEpisodesPageCommandHandler : IRequestHandler<SyncWorkEpisodesPageCommand>
{
    // Chặn vòng lặp vô hạn khi cấu hình phân trang sai (trang nguồn luôn báo còn trang sau).
    public const int MaxPages = 2000;

    private readonly IApplicationDbContext _context;
    private readonly ICrawlStoryRepositoryFactory _factory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SyncWorkEpisodesPageCommandHandler> _logger;

    public SyncWorkEpisodesPageCommandHandler(IApplicationDbContext context, ICrawlStoryRepositoryFactory factory,
        TimeProvider timeProvider, ILogger<SyncWorkEpisodesPageCommandHandler> logger)
    {
        _context = context;
        _factory = factory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Handle(SyncWorkEpisodesPageCommand request, CancellationToken cancellationToken)
    {
        var story = await _context.Works
            .Include(x => x.StorySource)
            .FirstOrDefaultAsync(x => x.Id == request.WorkId, cancellationToken);

        if (story is null) return;

        var storyUrl = story.StorySource?.LinkRaw;
        if (string.IsNullOrWhiteSpace(storyUrl))
        {
            await FailAsync(story, "Truyện không có nguồn truyện (link trang gốc) để lấy chương.", cancellationToken);
            return;
        }

        if (request.Page > MaxPages)
        {
            await FailAsync(story, $"Dừng ở trang {MaxPages}: kiểm tra cấu hình phân trang mục lục của nguồn web.", cancellationToken);
            return;
        }

        List<(string Link, string? Title)> items;
        bool hasNextPage;
        try
        {
            var source = await CrawlSource.LoadAsync(_context, story.CreatedBy, _factory, story.StorySource!.WebSourceId,
                cancellationToken);

            if (!CrawlSource.IsSameHost(storyUrl, source.Context.BaseUrl))
                throw new CrawlException("Link trang gốc của truyện không thuộc nguồn web.");

            var page = await source.Repository.FetchChapterListAsync(source.Context, storyUrl, request.Page, cancellationToken);
            items = page.Items
                .Where(x => !string.IsNullOrWhiteSpace(x.Link))
                .Select(x => (x.Link!, x.Title))
                .DistinctBy(x => x.Item1)
                .ToList();
            hasNextPage = page.HasNextPage;
        }
        catch (CrawlException ex) when (ex is not CrawlBlockedException)
        {
            // Không ném lại: lỗi của trang nguồn (cấu hình sai, bị chặn…) thử lại ngay cũng vô ích — người dùng xem lỗi
            // và bấm đồng bộ lại.
            await FailAsync(story, $"Trang mục lục {request.Page}: {ex.Message}", cancellationToken);
            return;
        }

        var links = items.Select(x => x.Link).ToList();
        var existing = (await _context.Episodes
                .Where(x => x.WorkId == story.Id && x.LinkRaw != null && links.Contains(x.LinkRaw))
                .Select(x => x.LinkRaw!)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var lastNumber = await _context.Episodes
            .Where(x => x.WorkId == story.Id)
            .MaxAsync(x => (int?)x.EpisodeNumber, cancellationToken) ?? 0;

        var newChapters = items
            .Where(x => !existing.Contains(x.Link))
            .Select(x => new Episode
            {
                WorkId = story.Id,
                LinkRaw = x.Link,
                Title = x.Title,
                NameRaw = x.Title,
                EpisodeNumber = ++lastNumber,
                Status = EpisodeStatus.Listed,
                CreatedBy = story.CreatedBy
            })
            .ToList();

        // Trang không có chương nào thì dừng dù trang nguồn báo còn trang sau (tránh lặp vô hạn).
        var continueToNextPage = hasNextPage && items.Count > 0;

        await _context.ExecuteInTransactionAsync(async ct =>
        {
            _context.Episodes.AddRange(newChapters);
            await _context.SaveChangesAsync(ct);

            foreach (var chapter in newChapters)
                chapter.AddIntegrationEvent(new ChapterFetchRequestedIntegrationEvent { ChapterId = chapter.Id }, ct);

            if (continueToNextPage)
                story.AddIntegrationEvent(
                    new StoryChaptersSyncRequestedIntegrationEvent { StoryId = story.Id, Page = request.Page + 1 }, ct);
            else
                story.EpisodesSyncedAt = _timeProvider.GetUtcNow();

            story.EpisodeSyncError = null;
            await _context.SaveChangesAsync(ct);
        }, cancellationToken);

        _logger.LogInformation("Story {StoryId} mục lục trang {Page}: {Total} chương, thêm mới {New}{Next}",
            story.Id, request.Page, items.Count, newChapters.Count, continueToNextPage ? ", còn trang sau" : ", xong");
    }

    private async Task FailAsync(Work story, string error, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Story {StoryId}: đồng bộ mục lục lỗi: {Error}", story.Id, error);
        story.EpisodeSyncError = error;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
