using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Local.AIs;
using SurveillanceCameras.Application.Local.Recovery;
using SurveillanceCameras.Application.Repository.AI;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.DTOs;
using SurveillanceCameras.Application.Repository.Tyt;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Data;

namespace SurveillanceCameras.Application.UnitTests.Recovery;

/// <summary>Worker khôi phục lỗi: chỉ đưa tác vụ lỗi đi lại khi nguyên nhân (IP, cookie, phiên TYT, AI) đã hết.</summary>
public class RecoverFailuresTests
{
    private const string Owner = "user-1";
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 3, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private ApplicationDbContext _db = null!;
    private Mock<ICrawlStoryRepository> _crawl = null!;
    private Mock<ITytRepository> _tyt = null!;
    private Mock<IAITextRepository> _ai = null!;
    private WebSource _webSource = null!;
    private Account _tytAccount = null!;
    private Work _work = null!;

    [SetUp]
    public async Task SetUp()
    {
        _db = TestDb.Create();
        _crawl = new Mock<ICrawlStoryRepository>();
        _tyt = new Mock<ITytRepository>();
        _ai = new Mock<IAITextRepository>();

        _webSource = new WebSource { Title = "wikicv", BaseUrl = "https://wikicv.test/list", CreatedBy = Owner };
        _tytAccount = new Account { AccountType = new AccountType { Title = "TYT", BaseUrl = "https://tyt.test/", CreatedBy = Owner }, CreatedBy = Owner };
        _db.AddRange(_webSource, _tytAccount);
        await _db.SaveChangesAsync();

        var source = new StorySource { WebSourceId = _webSource.Id, CreatedBy = Owner };
        _db.StorySources.Add(source);
        await _db.SaveChangesAsync();

        _work = new Work
        {
            Title = "Truyện", Type = WorkType.Story, CreatedBy = Owner, StorySourceId = source.Id, AccountId = _tytAccount.Id,
            TytStoryId = "0123456789abcdef01234567", Episodes = []
        };
        _db.Works.Add(_work);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<Episode> ChapterAsync(int number, EpisodeStatus status, string? uploadError = null, DateTimeOffset? queuedAt = null)
    {
        var chapter = new Episode
        {
            WorkId = _work.Id, EpisodeNumber = number, Status = status, CreatedBy = Owner, LinkRaw = $"https://wikicv.test/c/{number}",
            TytUploadError = uploadError, QueuedAt = queuedAt
        };
        _db.Episodes.Add(chapter);
        await _db.SaveChangesAsync();
        return chapter;
    }

    private async Task<IReadOnlyList<RecoveryGroupResult>> RunAsync(int maxPerGroup = 50)
    {
        var factory = new Mock<ICrawlStoryRepositoryFactory>();
        factory.Setup(x => x.GetRepository(It.IsAny<WebSource>())).Returns(_crawl.Object);
        var runner = new AIPromptRunner(_db, _ai.Object, NullLogger<AIPromptRunner>.Instance);

        return await new RecoverFailuresCommandHandler(_db, factory.Object, runner, _tyt.Object, new FixedTime(Now),
                NullLogger<RecoverFailuresCommandHandler>.Instance)
            .Handle(new RecoverFailuresCommand(maxPerGroup, TimeSpan.FromMinutes(60)), CancellationToken.None);
    }

    private void CrawlHealthy()
        => _crawl.Setup(x => x.FetchStoryListAsync(It.IsAny<CrawlContext>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrawlPageDto<StoryCrawlDto> { Items = [], TotalPages = 1 });

    [Test]
    public async Task FetchFailures_WhileTheIpIsBlocked_AreNotRetried_AndNothingIsSentToTheSource()
    {
        await ChapterAsync(1, EpisodeStatus.FetchFailed);
        _crawl.Setup(x => x.FetchStoryListAsync(It.IsAny<CrawlContext>(), 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CrawlBlockedException("chặn", Now.AddMinutes(10)));

        var result = await RunAsync();

        var group = result.ShouldHaveSingleItem();
        group.Healthy.ShouldBeFalse();
        group.Reason!.ShouldContain("IP đang bị chặn");
        group.Requeued.ShouldBe(0);
        _db.OutboxEventTypes().ShouldBeEmpty();
        // Chỉ đúng một request thử, không phải một request cho mỗi chương lỗi.
        _crawl.Verify(x => x.FetchStoryListAsync(It.IsAny<CrawlContext>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _crawl.VerifyNoOtherCalls();
    }

    [Test]
    public async Task FetchFailures_OnceTheSourceWorksAgain_AreRequeuedInBatches()
    {
        for (var i = 1; i <= 3; i++) await ChapterAsync(i, EpisodeStatus.FetchFailed);
        CrawlHealthy();

        var result = await RunAsync(maxPerGroup: 2);

        result.ShouldHaveSingleItem().Requeued.ShouldBe(2);
        _db.OutboxEventTypes().ShouldBe(["ChapterFetchRequestedIntegrationEvent", "ChapterFetchRequestedIntegrationEvent"]);
        _db.Episodes.Count(x => x.QueuedAt == Now).ShouldBe(2);

        // Lượt sau: 2 chương vừa vào hàng đợi chưa quá hạn chưa đưa lại, chỉ còn chương thứ 3.
        var again = await RunAsync(maxPerGroup: 2);
        again.ShouldHaveSingleItem().Requeued.ShouldBe(1);
    }

    [Test]
    public async Task ExpiredCookie_StopsTheGroup_ButTytChaptersOfAHealthyAccountStillRecover()
    {
        await ChapterAsync(1, EpisodeStatus.FetchFailed);
        await ChapterAsync(2, EpisodeStatus.Edited, uploadError: "lỗi tạm");
        _crawl.Setup(x => x.FetchStoryListAsync(It.IsAny<CrawlContext>(), 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CrawlException("Trang yêu cầu đăng nhập — cookie đã hết hạn"));

        var result = await RunAsync();

        result.Single(x => x.Group.StartsWith("Tải chương")).Healthy.ShouldBeFalse();
        result.Single(x => x.Group.StartsWith("Đăng chương lên TYT")).Requeued.ShouldBe(1);
        _db.OutboxEventTypes().ShouldBe(["ChapterTytUploadRequestedIntegrationEvent"]);
    }

    [Test]
    public async Task TytSessionExpired_PausesStoryAndChapterRetries_UntilLoginWorks()
    {
        await ChapterAsync(1, EpisodeStatus.Edited, uploadError: "chưa đăng nhập");
        _work.TytError = "Tài khoản chưa đăng nhập TYT";
        await _db.SaveChangesAsync();
        _tyt.Setup(x => x.CheckSessionAsync(It.IsAny<TytContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TytException("Tài khoản chưa đăng nhập TYT"));

        var blocked = await RunAsync();

        blocked.ShouldAllBe(x => !x.Healthy && x.Requeued == 0);
        blocked.Count.ShouldBe(2);
        _db.OutboxEventTypes().ShouldBeEmpty();
        (await _db.Works.FirstAsync()).TytError.ShouldNotBeNull();

        // Đăng nhập lại: chu kỳ sau xử lý cả truyện lẫn chương.
        _tyt.Setup(x => x.CheckSessionAsync(It.IsAny<TytContext>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var recovered = await RunAsync();

        recovered.ShouldAllBe(x => x.Healthy && x.Requeued == 1);
        _db.OutboxEventTypes().OrderBy(x => x).ShouldBe(["ChapterTytUploadRequestedIntegrationEvent", "TytStoryCreateRequestedIntegrationEvent"]);
        (await _db.Works.FirstAsync()).TytError.ShouldBeNull();
    }

    [Test]
    public async Task EditFailures_WaitUntilTheAiAnswersAgain()
    {
        await ChapterAsync(1, EpisodeStatus.EditFailed);
        var promptType = new PromptType { Title = "Biên tập", Code = "edit-chapter-content", CreatedBy = Owner };
        _db.Prompts.Add(new Prompt { Title = "p", Content = "hãy biên tập", PromptType = promptType, CreatedBy = Owner });
        _db.AIs.Add(new AI { Title = "gemini", Model = "m", ApiKey = "k", CreatedBy = Owner });
        await _db.SaveChangesAsync();

        _ai.Setup(x => x.GenerateAsync(It.IsAny<AITextRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AIProviderException("429 hết quota"));
        var quotaOut = await RunAsync();
        quotaOut.ShouldHaveSingleItem().Healthy.ShouldBeFalse();
        _db.OutboxEventTypes().ShouldBeEmpty();

        _ai.Setup(x => x.GenerateAsync(It.IsAny<AITextRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync("OK");
        var back = await RunAsync();
        back.ShouldHaveSingleItem().Requeued.ShouldBe(1);
        _db.OutboxEventTypes().ShouldBe(["ChapterEditRequestedIntegrationEvent"]);
    }

    [Test]
    public async Task NothingFailed_MeansNoProbeAtAll()
    {
        await ChapterAsync(1, EpisodeStatus.Edited);

        (await RunAsync()).ShouldBeEmpty();
        _crawl.VerifyNoOtherCalls();
        _tyt.VerifyNoOtherCalls();
    }
}
