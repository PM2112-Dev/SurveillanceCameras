using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Local.Episodes.Commands.ScheduleTytUploads;
using SurveillanceCameras.Application.Local.Tyt;
using SurveillanceCameras.Application.Local.Tyt.Commands.ActivateWaitingTytStories;
using SurveillanceCameras.Application.Local.Tyt.Commands.AdvanceTytPublish;
using SurveillanceCameras.Application.Local.Tyt.Commands.CheckTytApprovals;
using SurveillanceCameras.Application.Local.Works.Commands.CreateTytStory;
using SurveillanceCameras.Application.Repository.Tyt;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Data;

namespace SurveillanceCameras.Application.UnitTests.Tyt;

/// <summary>Quy trình xuất bản: giới hạn truyện mỗi tài khoản, đăng chương đầu, chờ duyệt, rồi mới đăng theo lịch.</summary>
public class TytPublishWorkflowTests
{
    private const string Owner = "user-1";
    private const string StoryId = "0123456789abcdef01234567";
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 3, 0, 0, TimeSpan.Zero);   // 10:00 giờ VN
    private static readonly TimeZoneInfo Vn = TimeZoneInfo.CreateCustomTimeZone("vn", TimeSpan.FromHours(7), "vn", "vn");

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private ApplicationDbContext _db = null!;
    private Mock<ITytRepository> _tyt = null!;
    private Account _account = null!;
    private IOptions<TytPolicyOptions> _policy = null!;

    [SetUp]
    public async Task SetUp()
    {
        _db = TestDb.Create();
        _tyt = new Mock<ITytRepository>();
        _policy = Options.Create(new TytPolicyOptions { MaxActiveStoriesPerAccount = 2, FirstChaptersToPublish = 3, RetryMinutes = 10 });
        _account = new Account { AccountType = new AccountType { Title = "TYT", BaseUrl = "https://tyt.test/", CreatedBy = Owner }, CreatedBy = Owner };
        _db.Accounts.Add(_account);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<Work> StoryAsync(TytStage stage, string? tytId = StoryId, bool isFull = false, int chapters = 5, int ready = 5,
        TytUploadMode mode = TytUploadMode.Hourly)
    {
        var work = new Work
        {
            Title = "Truyện", Type = WorkType.Story, CreatedBy = Owner, AccountId = _account.Id, TytStoryId = tytId, TytStage = stage,
            IsFull = isFull, TytUploadMode = mode, TytUploadBatchSize = 2, TytUploadEveryHours = 1, Episodes = []
        };
        for (var i = 1; i <= chapters; i++)
        {
            work.Episodes.Add(new Episode
            {
                EpisodeNumber = i, NameRaw = $"Chương {i}", CreatedBy = Owner, Status = i <= ready ? EpisodeStatus.Edited : EpisodeStatus.Fetched,
                ContentRaw = "gốc", ContentEdit = i <= ready ? "nội dung" : null
            });
        }

        _db.Works.Add(work);
        await _db.SaveChangesAsync();
        return work;
    }

    private AdvanceTytPublishCommandHandler Advance(DateTimeOffset? now = null)
        => new(_db, _tyt.Object, new FixedTime(now ?? Now), _policy, NullLogger<AdvanceTytPublishCommandHandler>.Instance);

    private ScheduleTytUploadsCommandHandler Scheduler(DateTimeOffset? now = null)
        => new(_db, new FixedTime(now ?? Now), NullLogger<ScheduleTytUploadsCommandHandler>.Instance);

    private async Task MarkQueuedUploadedAsync()
    {
        foreach (var chapter in _db.Episodes.Where(x => x.QueuedAt != null)) { chapter.IsUploaded = true; chapter.TytUploadedAt = chapter.QueuedAt; }
        await _db.SaveChangesAsync();
    }

    [Test]
    public async Task CreateStory_WhenTheAccountHasEnoughActiveStories_WaitsForASlot_WithoutCallingTyt()
    {
        await StoryAsync(TytStage.Approved, tytId: "a".PadRight(24, 'a'));
        await StoryAsync(TytStage.None, tytId: "b".PadRight(24, 'b'));
        var third = await StoryAsync(TytStage.None, tytId: null);

        await new CreateTytStoryCommandHandler(_db, _tyt.Object, new FixedTime(Now), _policy, NullLogger<CreateTytStoryCommandHandler>.Instance)
            .Handle(new CreateTytStoryCommand(third.Id), CancellationToken.None);

        var saved = await _db.Works.FirstAsync(x => x.Id == third.Id);
        saved.TytStage.ShouldBe(TytStage.WaitingSlot);
        saved.TytStoryId.ShouldBeNull();
        saved.TytError.ShouldBeNull();
        saved.TytStageNote!.ShouldContain("2/2");
        _tyt.VerifyNoOtherCalls();
    }

    [Test]
    public async Task CompletedStories_FreeASlot_ForTheOnesWaiting_InOrder()
    {
        var done = await StoryAsync(TytStage.Approved, tytId: "a".PadRight(24, 'a'));
        await StoryAsync(TytStage.Approved, tytId: "b".PadRight(24, 'b'));
        var first = await StoryAsync(TytStage.WaitingSlot, tytId: null);
        var second = await StoryAsync(TytStage.WaitingSlot, tytId: null);
        var handler = new ActivateWaitingTytStoriesCommandHandler(_db, _policy);

        (await handler.Handle(new ActivateWaitingTytStoriesCommand(), CancellationToken.None)).ShouldBe(0);   // đủ 2/2

        done.IsFull = true;   // truyện hoàn thành không chiếm chỗ
        await _db.SaveChangesAsync();
        (await handler.Handle(new ActivateWaitingTytStoriesCommand(), CancellationToken.None)).ShouldBe(1);

        (await _db.Works.FirstAsync(x => x.Id == first.Id)).TytStage.ShouldBe(TytStage.None);
        (await _db.Works.FirstAsync(x => x.Id == second.Id)).TytStage.ShouldBe(TytStage.WaitingSlot);
        _db.OutboxEventTypes().ShouldBe(["TytStoryCreateRequestedIntegrationEvent"]);
    }

    [Test]
    public async Task Publish_UploadsTheFirstChapters_ThenRequestsPublic_AndWaitsForApproval()
    {
        var work = await StoryAsync(TytStage.PublishRequested, ready: 2);   // mới 2/3 chương đầu xử lý xong
        _tyt.Setup(x => x.RequestPublishAsync(It.IsAny<TytContext>(), StoryId, It.IsAny<CancellationToken>())).ReturnsAsync("Đã gửi yêu cầu");

        (await Advance().Handle(new AdvanceTytPublishCommand(), CancellationToken.None)).ShouldBe(0);
        // Chương xong thì xếp hàng đăng, nhưng chưa gửi yêu cầu công khai.
        _db.Episodes.Count(x => x.QueuedAt != null).ShouldBe(2);
        _tyt.Verify(x => x.RequestPublishAsync(It.IsAny<TytContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        (await _db.Works.FirstAsync()).TytStageNote!.ShouldContain("0/3");

        // Chương thứ 3 xong, ba chương đầu đã lên TYT → gửi yêu cầu công khai.
        foreach (var chapter in _db.Episodes.Where(x => x.EpisodeNumber == 3)) { chapter.Status = EpisodeStatus.Edited; chapter.ContentEdit = "nd"; }
        await MarkQueuedUploadedAsync();
        await _db.SaveChangesAsync();
        await Advance(Now.AddMinutes(1)).Handle(new AdvanceTytPublishCommand(), CancellationToken.None);
        await MarkQueuedUploadedAsync();

        (await Advance(Now.AddMinutes(2)).Handle(new AdvanceTytPublishCommand(), CancellationToken.None)).ShouldBe(1);

        var saved = await _db.Works.FirstAsync(x => x.Id == work.Id);
        saved.TytStage.ShouldBe(TytStage.PendingApproval);
        saved.TytPublishRequestedAt.ShouldNotBeNull();
        _db.Episodes.Count(x => x.EpisodeNumber > 3 && x.QueuedAt != null).ShouldBe(0);   // chương 4, 5 chờ duyệt
    }

    [Test]
    public async Task Publish_WhenTytRefuses_StaysAndRetriesLater_NotEveryTick()
    {
        await StoryAsync(TytStage.PublishRequested);
        foreach (var chapter in _db.Episodes.Where(x => x.EpisodeNumber <= 3)) chapter.IsUploaded = true;
        await _db.SaveChangesAsync();
        _tyt.Setup(x => x.RequestPublishAsync(It.IsAny<TytContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TytException("chưa đăng nhập"));

        await Advance().Handle(new AdvanceTytPublishCommand(), CancellationToken.None);
        await Advance(Now.AddMinutes(1)).Handle(new AdvanceTytPublishCommand(), CancellationToken.None);   // trong 10 phút: không gửi lại

        _tyt.Verify(x => x.RequestPublishAsync(It.IsAny<TytContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        var saved = await _db.Works.FirstAsync();
        saved.TytStage.ShouldBe(TytStage.PublishRequested);
        saved.TytStageNote!.ShouldContain("chưa đăng nhập");
    }

    [Test]
    public async Task Approval_IsDetectedFromThePublicList_AndOnlyThenTheScheduleStarts()
    {
        var work = await StoryAsync(TytStage.PendingApproval);
        foreach (var chapter in _db.Episodes.Where(x => x.EpisodeNumber <= 3)) { chapter.IsUploaded = true; chapter.TytUploadedAt = Now.AddDays(-1); }
        await _db.SaveChangesAsync();

        // Chờ duyệt: lịch đăng không động tới truyện.
        (await Scheduler().Handle(new ScheduleTytUploadsCommand(Vn), CancellationToken.None)).ShouldBe(0);

        _tyt.Setup(x => x.GetStoriesAsync(It.IsAny<TytContext>(), TytStoryVisibility.Public, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TytStoryItemDto>());
        var check = new CheckTytApprovalsCommandHandler(_db, _tyt.Object, new FixedTime(Now), NullLogger<CheckTytApprovalsCommandHandler>.Instance);
        (await check.Handle(new CheckTytApprovalsCommand(), CancellationToken.None)).ShouldBe(0);
        (await _db.Works.FirstAsync()).TytStage.ShouldBe(TytStage.PendingApproval);

        _tyt.Setup(x => x.GetStoriesAsync(It.IsAny<TytContext>(), TytStoryVisibility.Public, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TytStoryItemDto> { new() { Id = StoryId, IsPublic = true } });
        (await check.Handle(new CheckTytApprovalsCommand(), CancellationToken.None)).ShouldBe(1);

        var approved = await _db.Works.FirstAsync(x => x.Id == work.Id);
        approved.TytStage.ShouldBe(TytStage.Approved);
        approved.TytApprovedAt.ShouldBe(Now);
        // Được duyệt: đăng chương 4–5 theo lịch.
        (await Scheduler().Handle(new ScheduleTytUploadsCommand(Vn), CancellationToken.None)).ShouldBe(2);
    }

    [Test]
    public async Task Approval_WhenTytIsUnreachable_KeepsWaiting_AndNotesWhy()
    {
        await StoryAsync(TytStage.PendingApproval);
        _tyt.Setup(x => x.GetStoriesAsync(It.IsAny<TytContext>(), It.IsAny<TytStoryVisibility>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TytException("phiên hết hạn"));

        await new CheckTytApprovalsCommandHandler(_db, _tyt.Object, new FixedTime(Now), NullLogger<CheckTytApprovalsCommandHandler>.Instance)
            .Handle(new CheckTytApprovalsCommand(), CancellationToken.None);

        var saved = await _db.Works.FirstAsync();
        saved.TytStage.ShouldBe(TytStage.PendingApproval);
        saved.TytStageNote!.ShouldContain("phiên hết hạn");
    }

    [Test]
    public async Task DailyLimit_CapsTheChaptersPerDay_AcrossBatches()
    {
        var work = await StoryAsync(TytStage.Approved, chapters: 10, ready: 10, mode: TytUploadMode.Daily);
        work.TytUploadDailyTime = "08:00,10:00,12:00";
        work.TytUploadDailyLimit = 3;
        work.TytUploadBatchSize = 2;
        await _db.SaveChangesAsync();

        // 10:00 giờ VN: lô 08:00 trước, 2 chương (hạn mức 3).
        (await Scheduler().Handle(new ScheduleTytUploadsCommand(Vn), CancellationToken.None)).ShouldBe(2);
        await MarkQueuedUploadedAsync();
        // 12:00: lô kế chỉ còn 1 chương cho đủ 3.
        (await Scheduler(Now.AddHours(2)).Handle(new ScheduleTytUploadsCommand(Vn), CancellationToken.None)).ShouldBe(1);
        await MarkQueuedUploadedAsync();
        // Sau 12:00 tối cùng ngày: hết hạn mức.
        (await Scheduler(Now.AddHours(10)).Handle(new ScheduleTytUploadsCommand(Vn), CancellationToken.None)).ShouldBe(0);
        // Sáng hôm sau: hạn mức mới.
        (await Scheduler(Now.AddHours(23)).Handle(new ScheduleTytUploadsCommand(Vn), CancellationToken.None)).ShouldBe(2);
    }

    [Test]
    public async Task NotApprovedStories_AreNeverScheduled_EvenInImmediateMode()
    {
        await StoryAsync(TytStage.PendingApproval, mode: TytUploadMode.Immediate);
        await StoryAsync(TytStage.None, tytId: "c".PadRight(24, 'c'), mode: TytUploadMode.Hourly);

        (await Scheduler().Handle(new ScheduleTytUploadsCommand(Vn), CancellationToken.None)).ShouldBe(0);
    }
}
