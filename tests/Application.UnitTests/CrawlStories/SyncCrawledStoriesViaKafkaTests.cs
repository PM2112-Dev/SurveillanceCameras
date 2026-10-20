using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.CrawlStories.Commands.SyncCrawledStories;
using SurveillanceCameras.Application.Local.CrawlStories.Commands.SyncStoryDetail;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.DTOs;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Infrastructure.Data;
using SurveillanceCameras.Infrastructure.Kafka;

namespace SurveillanceCameras.Application.UnitTests.CrawlStories;

/// <summary>Đồng bộ truyện đi qua Kafka: lưu + xếp hàng ngay, trang chi tiết lấy lần lượt ở nền.</summary>
public class SyncCrawledStoriesViaKafkaTests
{
    private const string Owner = "user-1";
    private const string Base = "https://truyen.test";

    private ApplicationDbContext _db = null!;
    private Mock<ICrawlStoryRepository> _crawl = null!;
    private Mock<ICrawlStoryRepositoryFactory> _factory = null!;
    private Mock<IUser> _user = null!;
    private WebSource _webSource = null!;

    [SetUp]
    public async Task SetUp()
    {
        _db = TestDb.Create();
        _crawl = new Mock<ICrawlStoryRepository>();
        _factory = new Mock<ICrawlStoryRepositoryFactory>();
        _factory.Setup(x => x.GetRepository(It.IsAny<WebSource>())).Returns(_crawl.Object);
        _user = new Mock<IUser>();
        _user.Setup(x => x.Id).Returns(Owner);
        _webSource = new WebSource
        {
            Title = "Nguồn", BaseUrl = Base + "/danh-sach", CreatedBy = Owner,
            CrawlConfig = """{ "story": { "title": "h1" } }"""
        };
        _db.WebSources.Add(_webSource);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<SyncCrawledStoriesResult> SyncAsync(params string[] names)
        => await new SyncCrawledStoriesCommandHandler(_db, _user.Object).Handle(new SyncCrawledStoriesCommand
        {
            WebSourceId = _webSource.Id,
            Stories = names.Select(n => new SyncCrawledStoryItem
                { Link = $"{Base}/truyen/{n}", Title = $"Truyện {n}", Author = "Tác giả", Status = "Còn tiếp", ChapterCount = 10 }).ToList()
        }, CancellationToken.None);

    private SyncStoryDetailCommandHandler DetailHandler()
        => new(_db, _factory.Object, NullLogger<SyncStoryDetailCommandHandler>.Instance);

    private async Task SetOwnerAsync()
    {
        foreach (var source in _db.StorySources.Where(x => x.CreatedBy == null)) source.CreatedBy = Owner;
        await _db.SaveChangesAsync();
    }

    [Test]
    public async Task Sync_SavesListInfoAndQueuesOneEventPerStory_WithoutCallingTheSource()
    {
        var result = await SyncAsync("a", "b", "b");   // trùng link chỉ tính một lần

        result.ShouldBe(new SyncCrawledStoriesResult(Created: 2, Updated: 0, Queued: 2));
        var saved = await _db.StorySources.OrderBy(x => x.Id).ToListAsync();
        saved.Select(x => x.Title).ShouldBe(["Truyện a", "Truyện b"]);
        saved.All(x => x.Description is null).ShouldBeTrue();   // chưa có chi tiết
        _db.OutboxEventTypes().ShouldBe(["StoryDetailSyncRequestedIntegrationEvent", "StoryDetailSyncRequestedIntegrationEvent"]);
        _crawl.VerifyNoOtherCalls();

        await SetOwnerAsync();   // TestDb không điền CreatedBy như khi chạy thật

        // Đồng bộ lại: cập nhật, không tạo trùng, vẫn xếp hàng lấy chi tiết.
        var again = await SyncAsync("a");
        again.ShouldBe(new SyncCrawledStoriesResult(Created: 0, Updated: 1, Queued: 1));
        (await _db.StorySources.CountAsync()).ShouldBe(2);
    }

    [Test]
    public async Task Detail_FillsDescription_AndOnlyNewSourcesGetCreatedEvent()
    {
        await SyncAsync("a");
        await SetOwnerAsync();
        _db.OutboxMessages.RemoveRange(_db.OutboxMessages);
        await _db.SaveChangesAsync();
        var source = await _db.StorySources.SingleAsync();
        _crawl.Setup(x => x.FetchStoryAsync(It.IsAny<CrawlContext>(), source.LinkRaw!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoryDataDto { Title = "Truyện A", Description = "Giới thiệu đầy đủ." });

        await DetailHandler().Handle(new SyncStoryDetailCommand(source.Id, IsNew: true), CancellationToken.None);

        (await _db.StorySources.SingleAsync()).Description.ShouldBe("Giới thiệu đầy đủ.");
        _db.OutboxEventTypes().ShouldBe(["StorySourceCreatedIntegrationEvent"]);

        // Nguồn đã có sẵn (đồng bộ lại): cập nhật chi tiết nhưng không tạo truyện lần nữa.
        _db.OutboxMessages.RemoveRange(_db.OutboxMessages);
        await _db.SaveChangesAsync();
        await DetailHandler().Handle(new SyncStoryDetailCommand(source.Id, IsNew: false), CancellationToken.None);
        _db.OutboxEventTypes().ShouldBeEmpty();
    }

    [Test]
    public async Task Detail_FailureFallsBackToListInfoAndStillCreatesTheStory()
    {
        await SyncAsync("a");
        await SetOwnerAsync();
        _db.OutboxMessages.RemoveRange(_db.OutboxMessages);
        await _db.SaveChangesAsync();
        _crawl.Setup(x => x.FetchStoryAsync(It.IsAny<CrawlContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CrawlException("Lỗi 500"));
        var source = await _db.StorySources.SingleAsync();

        await DetailHandler().Handle(new SyncStoryDetailCommand(source.Id, IsNew: true), CancellationToken.None);

        (await _db.StorySources.SingleAsync()).Title.ShouldBe("Truyện a");
        _db.OutboxEventTypes().ShouldBe(["StorySourceCreatedIntegrationEvent"]);
    }

    [Test]
    public async Task Detail_WhenTheSourceIsBlocking_ThrowsSoTheConsumerCanWaitAndRetry_WithoutCreatingAnything()
    {
        await SyncAsync("a");
        await SetOwnerAsync();
        _db.OutboxMessages.RemoveRange(_db.OutboxMessages);
        await _db.SaveChangesAsync();
        _crawl.Setup(x => x.FetchStoryAsync(It.IsAny<CrawlContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CrawlBlockedException("429", DateTimeOffset.UtcNow.AddMinutes(10)));
        var source = await _db.StorySources.SingleAsync();

        await Should.ThrowAsync<CrawlBlockedException>(() =>
            DetailHandler().Handle(new SyncStoryDetailCommand(source.Id, IsNew: true), CancellationToken.None));

        _db.OutboxEventTypes().ShouldBeEmpty();
    }

    [Test]
    public async Task BlockedRetry_WaitsThenSucceeds_AndGivesUpAfterTheMaximumWaits()
    {
        var waits = new List<TimeSpan>();
        var calls = 0;

        await CrawlBlockedRetry.RunAsync(_ =>
        {
            calls++;
            return calls < 3 ? throw new CrawlBlockedException("429", DateTimeOffset.UtcNow.AddMinutes(30)) : Task.CompletedTask;
        }, NullLogger.Instance, CancellationToken.None, (wait, _) => { waits.Add(wait); return Task.CompletedTask; });

        calls.ShouldBe(3);
        waits.Count.ShouldBe(2);
        waits.ShouldAllBe(w => w == CrawlBlockedRetry.MaxWait);   // chờ tối đa 10 phút mỗi lần dù nguồn đòi lâu hơn

        await Should.ThrowAsync<CrawlBlockedException>(() => CrawlBlockedRetry.RunAsync(
            _ => throw new CrawlBlockedException("429", DateTimeOffset.UtcNow.AddSeconds(5)),
            NullLogger.Instance, CancellationToken.None, (_, _) => Task.CompletedTask));
    }
}
