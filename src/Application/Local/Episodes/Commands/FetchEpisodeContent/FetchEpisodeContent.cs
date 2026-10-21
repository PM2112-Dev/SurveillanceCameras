using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;
using SurveillanceCameras.Application.Local.CrawlStories;
using SurveillanceCameras.Application.Local.WebSources;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Episodes.Commands.FetchEpisodeContent;

/// <summary>
/// Chạy nền: tải nội dung gốc của một chương rồi phát sự kiện cho AI biên tập — hoặc giữ nguyên nội dung gốc nếu nguồn
/// web không cần biên tập (<c>WebSource.IsNeedEdit</c>). Chương đã tải thì bỏ qua (chạy lặp an toàn).
/// Lỗi trang nguồn được ghi vào chương (FetchFailed) thay vì ném ra — không để hàng nghìn chương thử lại vô ích.
/// </summary>
public record FetchEpisodeContentCommand(int ChapterId) : IRequest;

public class FetchEpisodeContentCommandHandler : IRequestHandler<FetchEpisodeContentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICrawlStoryRepositoryFactory _factory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FetchEpisodeContentCommandHandler> _logger;

    public FetchEpisodeContentCommandHandler(IApplicationDbContext context, ICrawlStoryRepositoryFactory factory,
        TimeProvider timeProvider, ILogger<FetchEpisodeContentCommandHandler> logger)
    {
        _context = context;
        _factory = factory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Handle(FetchEpisodeContentCommand request, CancellationToken cancellationToken)
    {
        var chapter = await _context.Episodes
            .Include(x => x.Work!).ThenInclude(x => x.StorySource)
            .FirstOrDefaultAsync(x => x.Id == request.ChapterId, cancellationToken);

        if (chapter is null || string.IsNullOrWhiteSpace(chapter.LinkRaw)) return;

        if (chapter.Status is EpisodeStatus.Fetched or EpisodeStatus.Edited && !string.IsNullOrEmpty(chapter.ContentRaw))
            return;

        List<string> paragraphs;
        try
        {
            var webSourceId = chapter.Work?.StorySource?.WebSourceId
                              ?? throw new CrawlException("Truyện không có nguồn truyện để tải chương.");
            var source = await CrawlSource.LoadAsync(_context, chapter.Work!.CreatedBy, _factory, webSourceId, cancellationToken);

            if (!CrawlSource.IsSameHost(chapter.LinkRaw, source.Context.BaseUrl))
                throw new CrawlException("Link chương không thuộc nguồn web của truyện.");

            var content = await source.Repository.FetchChapterAsync(source.Context, chapter.LinkRaw, cancellationToken);
            paragraphs = content.Paragraphs;
            if (!string.IsNullOrWhiteSpace(content.Title)) chapter.NameRaw = content.Title;
        }
        catch (CrawlException ex) when (ex is not CrawlBlockedException)
        {
            _logger.LogWarning("Chapter {ChapterId}: tải nội dung lỗi: {Error}", chapter.Id, ex.Message);
            chapter.Status = EpisodeStatus.FetchFailed;
            chapter.Error = ex.Message;
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (paragraphs.Count == 0)
        {
            chapter.Status = EpisodeStatus.FetchFailed;
            chapter.Error = "Trang chương không có nội dung (kiểm tra selector nội dung chương hoặc cookie).";
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        chapter.ContentRaw = string.Join('\n', paragraphs);
        chapter.Status = EpisodeStatus.Fetched;
        chapter.FetchedAt = _timeProvider.GetUtcNow();
        chapter.Error = null;

        if (await WebSourceEditPolicy.IsNeedEditAsync(_context, chapter.Work?.StorySource?.WebSourceId, cancellationToken))
            chapter.AddIntegrationEvent(new ChapterEditRequestedIntegrationEvent { ChapterId = chapter.Id }, cancellationToken);
        else
        {
            EpisodeContentStatus.KeepOriginal(chapter, chapter.FetchedAt.Value);
            await TytUploadQueue.QueueIfImmediateAsync(_context, chapter, chapter.Work, _timeProvider.GetUtcNow(), cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
