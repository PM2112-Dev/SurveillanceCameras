using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Local.AIs;
using SurveillanceCameras.Application.Local.Episodes.Commands.EditEpisodeContent;
using SurveillanceCameras.Application.Local.Episodes.Commands.FetchEpisodeContent;
using SurveillanceCameras.Application.Local.Episodes.Commands.SyncWorkEpisodesPage;
using SurveillanceCameras.Application.Repository.AI;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.DTOs;
using SurveillanceCameras.Domain.Constants;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Data;

namespace SurveillanceCameras.Application.UnitTests.Episodes;

public class EpisodePipelineTests
{
    private const string Owner = "user-1";
    private const string StoryUrl = "https://truyen.test/truyen-a";

    private ApplicationDbContext _db = null!;
    private Mock<ICrawlStoryRepository> _crawl = null!;
    private Mock<IAITextRepository> _ai = null!;
    private Work _story = null!;

    [SetUp]
    public async Task SetUp()
    {
        _db = TestDb.Create();
        _crawl = new Mock<ICrawlStoryRepository>();
        _ai = new Mock<IAITextRepository>();

        var webSource = new WebSource
        {
            Title = "Nguồn", BaseUrl = "https://truyen.test/danh-sach", CrawlConfig = "{}", CreatedBy = Owner
        };
        var source = new StorySource { Title = "Truyện A", LinkRaw = StoryUrl, CreatedBy = Owner };
        _db.WebSources.Add(webSource);
        await _db.SaveChangesAsync();
        source.WebSourceId = webSource.Id;
        _db.StorySources.Add(source);
        await _db.SaveChangesAsync();

        _story = new Work { Title = "Truyện A", StorySourceId = source.Id, CreatedBy = Owner };
        _db.Works.Add(_story);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private ICrawlStoryRepositoryFactory Factory()
    {
        var factory = new Mock<ICrawlStoryRepositoryFactory>();
        factory.Setup(x => x.GetRepository(It.IsAny<WebSource>())).Returns(_crawl.Object);
        return factory.Object;
    }

    private SyncWorkEpisodesPageCommandHandler SyncHandler()
        => new(_db, Factory(), TimeProvider.System, NullLogger<SyncWorkEpisodesPageCommandHandler>.Instance);

    private void SetupChapterPage(int page, bool hasNext, params string[] slugs)
        => _crawl.Setup(x => x.FetchChapterListAsync(It.IsAny<CrawlContext>(), StoryUrl, page, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrawlPageDto<ChapterDataDto>
            {
                Page = page,
                HasNextPage = hasNext,
                Items = slugs.Select(s => new ChapterDataDto { Title = "Chương " + s, Link = $"{StoryUrl}/{s}" }).ToList()
            });

    [Test]
    public async Task SyncPage_AddsChaptersInOrder_QueuesFetchPerChapter_AndNextPage()
    {
        SetupChapterPage(1, hasNext: true, "c1", "c2", "c3");

        await SyncHandler().Handle(new SyncWorkEpisodesPageCommand(_story.Id, 1), CancellationToken.None);

        var chapters = await _db.Episodes.OrderBy(x => x.EpisodeNumber).ToListAsync();
        chapters.Select(x => x.EpisodeNumber).ShouldBe([1, 2, 3]);
        chapters.ShouldAllBe(x => x.Status == EpisodeStatus.Listed && x.CreatedBy == Owner);

        var events = _db.OutboxEventTypes();
        events.Count(x => x == "ChapterFetchRequestedIntegrationEvent").ShouldBe(3);
        events.Count(x => x == "StoryChaptersSyncRequestedIntegrationEvent").ShouldBe(1);   // trang 2
    }

    [Test]
    public async Task SyncAgain_OnlyAddsNewChapters_ContinuingNumbering_AndMarksSyncDone()
    {
        SetupChapterPage(1, hasNext: false, "c1", "c2");
        await SyncHandler().Handle(new SyncWorkEpisodesPageCommand(_story.Id, 1), CancellationToken.None);

        SetupChapterPage(1, hasNext: false, "c1", "c2", "c3");
        await SyncHandler().Handle(new SyncWorkEpisodesPageCommand(_story.Id, 1), CancellationToken.None);

        var chapters = await _db.Episodes.OrderBy(x => x.EpisodeNumber).ToListAsync();
        chapters.Count.ShouldBe(3);
        chapters[2].LinkRaw.ShouldEndWith("/c3");
        chapters[2].EpisodeNumber.ShouldBe(3);
        (await _db.Works.SingleAsync()).EpisodesSyncedAt.ShouldNotBeNull();
        _db.OutboxEventTypes().ShouldNotContain("StoryChaptersSyncRequestedIntegrationEvent");
    }

    [Test]
    public async Task SyncPage_EmptyPage_StopsEvenIfSourceSaysMorePages()
    {
        SetupChapterPage(1, hasNext: true);

        await SyncHandler().Handle(new SyncWorkEpisodesPageCommand(_story.Id, 1), CancellationToken.None);

        _db.OutboxEventTypes().ShouldBeEmpty();
    }

    [Test]
    public async Task SyncPage_CrawlError_IsSavedOnStory_NotThrown()
    {
        _crawl.Setup(x => x.FetchChapterListAsync(It.IsAny<CrawlContext>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CrawlException("bị chặn 403"));

        await SyncHandler().Handle(new SyncWorkEpisodesPageCommand(_story.Id, 1), CancellationToken.None);

        (await _db.Works.SingleAsync()).EpisodeSyncError!.ShouldContain("bị chặn 403");
    }

    private async Task<Episode> SeedChapterAsync(EpisodeStatus status = EpisodeStatus.Listed, string? content = null)
    {
        var chapter = new Episode
        {
            WorkId = _story.Id, EpisodeNumber = 1, NameRaw = "Chương 1", LinkRaw = $"{StoryUrl}/c1",
            Status = status, ContentRaw = content, CreatedBy = Owner
        };
        _db.Episodes.Add(chapter);
        await _db.SaveChangesAsync();
        return chapter;
    }

    private FetchEpisodeContentCommandHandler FetchHandler()
        => new(_db, Factory(), TimeProvider.System, NullLogger<FetchEpisodeContentCommandHandler>.Instance);

    [Test]
    public async Task Fetch_SavesRawContent_AndQueuesEdit()
    {
        var chapter = await SeedChapterAsync();
        _crawl.Setup(x => x.FetchChapterAsync(It.IsAny<CrawlContext>(), chapter.LinkRaw!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChapterContentDto { Title = "Chương 1: Mở đầu", Paragraphs = ["Đoạn 1", "Đoạn 2"] });

        await FetchHandler().Handle(new FetchEpisodeContentCommand(chapter.Id), CancellationToken.None);

        var saved = await _db.Episodes.SingleAsync();
        saved.Status.ShouldBe(EpisodeStatus.Fetched);
        saved.ContentRaw.ShouldBe("Đoạn 1\nĐoạn 2");
        saved.NameRaw.ShouldBe("Chương 1: Mở đầu");
        _db.OutboxEventTypes().ShouldBe(["ChapterEditRequestedIntegrationEvent"]);
    }

    [Test]
    public async Task Fetch_AlreadyFetched_IsSkipped()
    {
        var chapter = await SeedChapterAsync(EpisodeStatus.Fetched, "đã có");

        await FetchHandler().Handle(new FetchEpisodeContentCommand(chapter.Id), CancellationToken.None);

        _crawl.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Fetch_Error_MarksChapterFailed_NotThrown()
    {
        var chapter = await SeedChapterAsync();
        _crawl.Setup(x => x.FetchChapterAsync(It.IsAny<CrawlContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CrawlException("timeout"));

        await FetchHandler().Handle(new FetchEpisodeContentCommand(chapter.Id), CancellationToken.None);

        var saved = await _db.Episodes.SingleAsync();
        saved.Status.ShouldBe(EpisodeStatus.FetchFailed);
        saved.Error.ShouldBe("timeout");
    }

    private async Task DisableEditAsync()
    {
        (await _db.WebSources.SingleAsync()).IsNeedEdit = false;
        await _db.SaveChangesAsync();
    }

    [Test]
    public async Task Fetch_SourceWithoutEdit_KeepsOriginalAsEdited_WithoutQueuingEdit()
    {
        await DisableEditAsync();
        var chapter = await SeedChapterAsync();
        _crawl.Setup(x => x.FetchChapterAsync(It.IsAny<CrawlContext>(), chapter.LinkRaw!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChapterContentDto { Paragraphs = ["Đoạn 1", "Đoạn 2"] });

        await FetchHandler().Handle(new FetchEpisodeContentCommand(chapter.Id), CancellationToken.None);

        var saved = await _db.Episodes.SingleAsync();
        saved.Status.ShouldBe(EpisodeStatus.Edited);
        saved.ContentEdit.ShouldBe("Đoạn 1\nĐoạn 2");
        saved.PromptId.ShouldBeNull();
        saved.EditedAt.ShouldNotBeNull();
        _db.OutboxEventTypes().ShouldBeEmpty();
    }

    private EditEpisodeContentCommandHandler EditHandler()
        => new(_db, new AIPromptRunner(_db, _ai.Object, NullLogger<AIPromptRunner>.Instance), TimeProvider.System,
            NullLogger<EditEpisodeContentCommandHandler>.Instance);

    private async Task SeedEditSetupAsync()
    {
        var type = new PromptType { Code = PromptTypeCodes.EditChapterContent, Title = "Biên tập chương", CreatedBy = Owner };
        _db.Prompts.Add(new Prompt { Title = "Viết lại", Content = "Viết lại cho tự nhiên", PromptType = type, CreatedBy = Owner });
        _db.AIs.Add(new AI { Title = "Gemini", Provider = AIProvider.Gemini, Model = "m", ApiKey = "k", CreatedBy = Owner });
        await _db.SaveChangesAsync();
    }

    [Test]
    public async Task Edit_SavesEditedContent()
    {
        await SeedEditSetupAsync();
        var chapter = await SeedChapterAsync(EpisodeStatus.Fetched, "Hắn đi tới trước.");
        _ai.Setup(x => x.GenerateAsync(It.IsAny<AITextRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(" Hắn bước lên. ");

        await EditHandler().Handle(new EditEpisodeContentCommand(chapter.Id), CancellationToken.None);

        var saved = await _db.Episodes.SingleAsync();
        saved.Status.ShouldBe(EpisodeStatus.Edited);
        saved.ContentEdit.ShouldBe("Hắn bước lên.");
        saved.PromptId.ShouldNotBeNull();
    }

    [Test]
    public async Task Edit_LongChapter_IsSplitIntoChunks()
    {
        await SeedEditSetupAsync();
        var paragraph = new string('a', 3000);
        var chapter = await SeedChapterAsync(EpisodeStatus.Fetched, string.Join('\n', Enumerable.Repeat(paragraph, 6)));
        _ai.Setup(x => x.GenerateAsync(It.IsAny<AITextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AITextRequest r, CancellationToken _) => $"phần({r.Input.Length})");

        await EditHandler().Handle(new EditEpisodeContentCommand(chapter.Id), CancellationToken.None);

        // 6 đoạn × 3000 ký tự, tối đa 8000 ký tự/lần → 3 lần gọi, ghép lại theo thứ tự.
        _ai.Verify(x => x.GenerateAsync(It.IsAny<AITextRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        (await _db.Episodes.SingleAsync()).ContentEdit!.Split('\n').Length.ShouldBe(3);
    }

    [Test]
    public async Task Edit_AIFails_MarksChapterFailed_NotThrown()
    {
        await SeedEditSetupAsync();
        var chapter = await SeedChapterAsync(EpisodeStatus.Fetched, "nội dung");
        _ai.Setup(x => x.GenerateAsync(It.IsAny<AITextRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AIProviderException("429 quota"));

        await EditHandler().Handle(new EditEpisodeContentCommand(chapter.Id), CancellationToken.None);

        var saved = await _db.Episodes.SingleAsync();
        saved.Status.ShouldBe(EpisodeStatus.EditFailed);
        saved.Error!.ShouldContain("429 quota");
    }

    [Test]
    public async Task Edit_NoPrompt_KeepsRawContent_WithHint()
    {
        var chapter = await SeedChapterAsync(EpisodeStatus.Fetched, "nội dung");

        await EditHandler().Handle(new EditEpisodeContentCommand(chapter.Id), CancellationToken.None);

        var saved = await _db.Episodes.SingleAsync();
        saved.Status.ShouldBe(EpisodeStatus.Fetched);
        saved.Error!.ShouldContain(PromptTypeCodes.EditChapterContent);
        _ai.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Edit_AlreadyEdited_SkippedUnlessForced()
    {
        await SeedEditSetupAsync();
        var chapter = await SeedChapterAsync(EpisodeStatus.Edited, "nội dung");
        _ai.Setup(x => x.GenerateAsync(It.IsAny<AITextRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync("mới");

        await EditHandler().Handle(new EditEpisodeContentCommand(chapter.Id), CancellationToken.None);
        _ai.VerifyNoOtherCalls();

        await EditHandler().Handle(new EditEpisodeContentCommand(chapter.Id, Force: true), CancellationToken.None);
        (await _db.Episodes.SingleAsync()).ContentEdit.ShouldBe("mới");
    }

    [Test]
    public async Task Edit_SourceWithoutEdit_KeepsOriginal_UnlessForced()
    {
        await SeedEditSetupAsync();
        await DisableEditAsync();
        var chapter = await SeedChapterAsync(EpisodeStatus.Fetched, "nội dung gốc");
        _ai.Setup(x => x.GenerateAsync(It.IsAny<AITextRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync("mới");

        // Chương đã tải trước khi tắt biên tập (vd. bấm "Xử lý lại chương dở") → giữ nguyên gốc, không gọi AI.
        await EditHandler().Handle(new EditEpisodeContentCommand(chapter.Id), CancellationToken.None);
        var saved = await _db.Episodes.SingleAsync();
        saved.Status.ShouldBe(EpisodeStatus.Edited);
        saved.ContentEdit.ShouldBe("nội dung gốc");
        _ai.VerifyNoOtherCalls();

        // Người dùng chủ động yêu cầu biên tập chương này thì vẫn dùng AI.
        await EditHandler().Handle(new EditEpisodeContentCommand(chapter.Id, Force: true), CancellationToken.None);
        (await _db.Episodes.SingleAsync()).ContentEdit.ShouldBe("mới");
    }

    [Test]
    public void SplitIntoChunks_KeepsAllTextInOrder()
    {
        var content = "ab\ncdefgh\nij";

        var chunks = EditEpisodeContentCommandHandler.SplitIntoChunks(content, 5);

        chunks.ShouldAllBe(c => c.Length <= 5);
        string.Concat(chunks).Replace("\n", "").ShouldBe("abcdefghij");
    }
}
