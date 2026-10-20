using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Local.AIs;
using SurveillanceCameras.Application.Local.Episodes.Commands.EditEpisodeContent;
using SurveillanceCameras.Application.Local.Episodes.Commands.RequestTytChapterUploads;
using SurveillanceCameras.Application.Local.Episodes.Commands.ScheduleTytUploads;
using SurveillanceCameras.Application.Local.Episodes.Commands.UploadTytChapter;
using SurveillanceCameras.Application.Repository.AI;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.Tyt;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Data;

namespace SurveillanceCameras.Application.UnitTests.Tyt;

/// <summary>Đăng chương lên TYT: xếp hàng theo lịch (ngay / giờ / ngày), thứ tự chương, và kết quả đăng.</summary>
public class TytChapterUploadTests
{
    private const string Owner = "user-1";
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 3, 0, 0, TimeSpan.Zero);   // 10:00 giờ VN
    private static readonly TimeZoneInfo Vn = TimeZoneInfo.CreateCustomTimeZone("vn", TimeSpan.FromHours(7), "vn", "vn");

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private ApplicationDbContext _db = null!;
    private Account _account = null!;

    [SetUp]
    public async Task SetUp()
    {
        _db = TestDb.Create();
        var type = new AccountType { Title = "TYT", BaseUrl = "https://tyt.test/", CreatedBy = Owner };
        _account = new Account { AccountType = type, CreatedBy = Owner };
        _db.Accounts.Add(_account);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<Work> StoryAsync(TytUploadMode mode, int chapters = 5, int ready = 5, int batch = 2,
        string? tytStoryId = "0123456789abcdef01234567")
    {
        var work = new Work
        {
            Title = "Truyện", Type = WorkType.Story, CreatedBy = Owner, AccountId = _account.Id, TytStoryId = tytStoryId,
            TytUploadMode = mode, TytUploadBatchSize = batch, TytUploadEveryHours = 1, Episodes = []
        };
        for (var i = 1; i <= chapters; i++)
        {
            work.Episodes.Add(new Episode
            {
                EpisodeNumber = i, NameRaw = $"Chương {i}", CreatedBy = Owner,
                Status = i <= ready ? EpisodeStatus.Edited : EpisodeStatus.Fetched,
                ContentRaw = "gốc", ContentEdit = i <= ready ? "nội dung" : null
            });
        }

        _db.Works.Add(work);
        await _db.SaveChangesAsync();
        return work;
    }

    private ScheduleTytUploadsCommandHandler Scheduler(DateTimeOffset? now = null)
        => new(_db, new FixedTime(now ?? Now), NullLogger<ScheduleTytUploadsCommandHandler>.Instance);

    private Task<int> ScheduleAsync(DateTimeOffset? now = null)
        => Scheduler(now).Handle(new ScheduleTytUploadsCommand(Vn), CancellationToken.None);

    /// <summary>Giả lập consumer đã đăng xong các chương đang trong hàng đợi.</summary>
    private async Task MarkQueuedAsUploadedAsync()
    {
        foreach (var chapter in _db.Episodes.Where(x => x.QueuedAt != null)) { chapter.IsUploaded = true; chapter.TytUploadedAt = chapter.QueuedAt; }
        await _db.SaveChangesAsync();
    }

    private List<int> QueuedNumbers()
        => _db.Episodes.Where(x => x.QueuedAt != null).OrderBy(x => x.EpisodeNumber).Select(x => x.EpisodeNumber).ToList();

    [Test]
    public async Task Hourly_QueuesOneBatchInChapterOrder_ThenWaitsForTheNextHour()
    {
        await StoryAsync(TytUploadMode.Hourly, batch: 2);

        (await ScheduleAsync()).ShouldBe(2);
        QueuedNumbers().ShouldBe([1, 2]);
        _db.OutboxEventTypes().ShouldBe(["ChapterTytUploadRequestedIntegrationEvent", "ChapterTytUploadRequestedIntegrationEvent"]);

        // Chưa đủ một giờ → chưa tới lượt.
        (await ScheduleAsync(Now.AddMinutes(59))).ShouldBe(0);
        // Đủ một giờ và hai chương đầu đã lên TYT: lô kế tiếp là chương 3–4.
        await MarkQueuedAsUploadedAsync();
        (await ScheduleAsync(Now.AddMinutes(61))).ShouldBe(2);
        QueuedNumbers().ShouldBe([1, 2, 3, 4]);
    }

    [Test]
    public async Task Manual_AndStoriesNotOnTyt_AreNeverScheduled()
    {
        await StoryAsync(TytUploadMode.Manual);
        await StoryAsync(TytUploadMode.Immediate, tytStoryId: null);

        (await ScheduleAsync()).ShouldBe(0);
        QueuedNumbers().ShouldBeEmpty();
    }

    [Test]
    public async Task Queue_StopsAtTheFirstChapterThatIsNotReady_SoOrderIsKept()
    {
        await StoryAsync(TytUploadMode.Immediate, chapters: 5, ready: 2);

        (await ScheduleAsync()).ShouldBe(2);   // chương 3 chưa biên tập xong → 4, 5 chờ
        QueuedNumbers().ShouldBe([1, 2]);
    }

    [Test]
    public async Task Queue_StopsAtAChapterWithAnUploadError_UntilItIsRecovered()
    {
        var work = await StoryAsync(TytUploadMode.Immediate, chapters: 3, ready: 3);
        var first = await _db.Episodes.FirstAsync(x => x.WorkId == work.Id && x.EpisodeNumber == 1);
        first.TytUploadError = "TYT từ chối";
        await _db.SaveChangesAsync();

        (await ScheduleAsync()).ShouldBe(0);
    }

    [Test]
    public async Task Daily_QueuesOnlyAfterTheScheduledTime_AndOncePerDay()
    {
        var work = await StoryAsync(TytUploadMode.Daily, batch: 1);
        work.TytUploadDailyTime = "11:00";   // giờ VN; bây giờ là 10:00
        await _db.SaveChangesAsync();

        (await ScheduleAsync()).ShouldBe(0);
        (await ScheduleAsync(Now.AddHours(1))).ShouldBe(1);   // 11:00 VN
        (await ScheduleAsync(Now.AddHours(5))).ShouldBe(0);   // cùng ngày, đã đăng lô hôm nay
        await MarkQueuedAsUploadedAsync();
        (await ScheduleAsync(Now.AddHours(25))).ShouldBe(1);  // sáng hôm sau, sau 11:00 VN
        QueuedNumbers().ShouldBe([1, 2]);
    }

    [Test]
    public async Task RequestUploads_QueuesTheNextChaptersNow_AndClearsPreviousErrors()
    {
        var work = await StoryAsync(TytUploadMode.Manual, chapters: 4, ready: 4);
        var failed = await _db.Episodes.FirstAsync(x => x.WorkId == work.Id && x.EpisodeNumber == 1);
        failed.TytUploadError = "lỗi cũ";
        await _db.SaveChangesAsync();

        var user = new Mock<IUser>();
        user.Setup(x => x.Id).Returns(Owner);
        var handler = new RequestTytChapterUploadsCommandHandler(_db, user.Object, new FixedTime(Now));

        (await handler.Handle(new RequestTytChapterUploadsCommand(work.Id, 3), CancellationToken.None)).ShouldBe(3);
        QueuedNumbers().ShouldBe([1, 2, 3]);
        (await _db.Episodes.FirstAsync(x => x.Id == failed.Id)).TytUploadError.ShouldBeNull();
    }

    [Test]
    public async Task Upload_Success_MarksChapterUploaded_AndStoresTytId()
    {
        var work = await StoryAsync(TytUploadMode.Manual, chapters: 1, ready: 1);
        var chapter = await _db.Episodes.FirstAsync(x => x.WorkId == work.Id);
        var tyt = new Mock<ITytRepository>();
        tyt.Setup(x => x.CreateChapterAsync(It.IsAny<TytContext>(), work.TytStoryId!, It.IsAny<TytChapterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("chapter-77");

        await new UploadTytChapterCommandHandler(_db, tyt.Object, new FixedTime(Now), NullLogger<UploadTytChapterCommandHandler>.Instance)
            .Handle(new UploadTytChapterCommand(chapter.Id), CancellationToken.None);

        var saved = await _db.Episodes.Include(x => x.Work).FirstAsync(x => x.Id == chapter.Id);
        saved.IsUploaded.ShouldBeTrue();
        saved.TytChapterId.ShouldBe("chapter-77");
        saved.TytUploadedAt.ShouldBe(Now);
        saved.TytUploadError.ShouldBeNull();
        saved.Work!.ChapterUploaded.ShouldBe(1);
        tyt.Verify(x => x.CreateChapterAsync(It.IsAny<TytContext>(), It.IsAny<string>(),
            It.Is<TytChapterRequest>(r => r.Title == "Chương 1" && r.Content == "nội dung" && r.Number == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Upload_TytError_IsRecordedOnTheChapter_AndNotThrown()
    {
        var work = await StoryAsync(TytUploadMode.Manual, chapters: 1, ready: 1);
        var chapter = await _db.Episodes.FirstAsync(x => x.WorkId == work.Id);
        var tyt = new Mock<ITytRepository>();
        tyt.Setup(x => x.CreateChapterAsync(It.IsAny<TytContext>(), It.IsAny<string>(), It.IsAny<TytChapterRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TytException("Tài khoản chưa đăng nhập TYT"));

        await new UploadTytChapterCommandHandler(_db, tyt.Object, new FixedTime(Now), NullLogger<UploadTytChapterCommandHandler>.Instance)
            .Handle(new UploadTytChapterCommand(chapter.Id), CancellationToken.None);

        var saved = await _db.Episodes.FirstAsync(x => x.Id == chapter.Id);
        saved.IsUploaded.ShouldBeFalse();
        saved.TytUploadError.ShouldBe("Tài khoản chưa đăng nhập TYT");
    }

    [Test]
    public async Task Upload_AlreadyUploadedChapter_IsSkipped()
    {
        var work = await StoryAsync(TytUploadMode.Manual, chapters: 1, ready: 1);
        var chapter = await _db.Episodes.FirstAsync(x => x.WorkId == work.Id);
        chapter.IsUploaded = true;
        await _db.SaveChangesAsync();
        var tyt = new Mock<ITytRepository>(MockBehavior.Strict);

        await new UploadTytChapterCommandHandler(_db, tyt.Object, new FixedTime(Now), NullLogger<UploadTytChapterCommandHandler>.Instance)
            .Handle(new UploadTytChapterCommand(chapter.Id), CancellationToken.None);

        tyt.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ImmediateMode_QueuesTheChapterAsSoonAsProcessingFinishes_ButOnlyWhenEarlierOnesAreUploaded()
    {
        var work = await StoryAsync(TytUploadMode.Immediate, chapters: 2, ready: 0);
        // Nguồn không cần AI biên tập → xử lý xong ngay khi gặp lệnh biên tập.
        var source = new WebSource { Title = "n", BaseUrl = "https://x.test", IsNeedEdit = false, CreatedBy = Owner };
        _db.WebSources.Add(source);
        await _db.SaveChangesAsync();
        var story = new StorySource { WebSourceId = source.Id, CreatedBy = Owner };
        _db.StorySources.Add(story);
        await _db.SaveChangesAsync();
        work.StorySourceId = story.Id;
        await _db.SaveChangesAsync();

        var runner = new AIPromptRunner(_db, new Mock<IAITextRepository>().Object, NullLogger<AIPromptRunner>.Instance);
        var handler = new EditEpisodeContentCommandHandler(_db, runner, new FixedTime(Now), NullLogger<EditEpisodeContentCommandHandler>.Instance);
        var second = await _db.Episodes.FirstAsync(x => x.WorkId == work.Id && x.EpisodeNumber == 2);
        var first = await _db.Episodes.FirstAsync(x => x.WorkId == work.Id && x.EpisodeNumber == 1);

        // Chương 2 xong trước chương 1: chưa đăng (chương 1 chưa lên TYT) — để lượt quét đăng đúng thứ tự.
        await handler.Handle(new EditEpisodeContentCommand(second.Id), CancellationToken.None);
        _db.OutboxEventTypes().ShouldBeEmpty();

        await handler.Handle(new EditEpisodeContentCommand(first.Id), CancellationToken.None);
        _db.OutboxEventTypes().ShouldBe(["ChapterTytUploadRequestedIntegrationEvent"]);
        (await _db.Episodes.FirstAsync(x => x.Id == first.Id)).QueuedAt.ShouldBe(Now);
    }
}
