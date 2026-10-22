using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Local.Tyt.Commands.SyncTytEarnings;
using SurveillanceCameras.Application.Repository.Tyt;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Data;

namespace SurveillanceCameras.Application.UnitTests.Tyt;

/// <summary>Chỉ truyện đã được duyệt và đủ điều kiện mới bật kiếm tiền và lấy được thu nhập.</summary>
public class TytEarningSyncTests
{
    private const string Owner = "user-1";
    private const string StoryId = "0123456789abcdef01234567";
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 3, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private ApplicationDbContext _db = null!;
    private Mock<ITytRepository> _tyt = null!;

    [SetUp]
    public async Task SetUp()
    {
        _db = TestDb.Create();
        _tyt = new Mock<ITytRepository>();
        var account = new Account { AccountType = new AccountType { Title = "TYT", BaseUrl = "https://tyt.test/", CreatedBy = Owner }, CreatedBy = Owner };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        _db.Works.AddRange(
            new Work { Title = "Đã duyệt", Type = WorkType.Story, CreatedBy = Owner, AccountId = account.Id, TytStoryId = StoryId, TytStage = TytStage.Approved },
            new Work { Title = "Chờ duyệt", Type = WorkType.Story, CreatedBy = Owner, AccountId = account.Id, TytStoryId = "b".PadRight(24, 'b'), TytStage = TytStage.PendingApproval });
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private Task<int> RunAsync()
        => new SyncTytEarningsCommandHandler(_db, _tyt.Object, new FixedTime(Now), NullLogger<SyncTytEarningsCommandHandler>.Instance)
            .Handle(new SyncTytEarningsCommand(), CancellationToken.None);

    [Test]
    public async Task NotYetEligible_NeitherEnablesEarningNorTouchesTheFigures_AndPendingStoriesAreNotChecked()
    {
        _tyt.Setup(x => x.GetEarningAsync(It.IsAny<TytContext>(), StoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TytEarningDto(false, false, null, null, null, null, null, null, null));

        (await RunAsync()).ShouldBe(0);

        var work = await _db.Works.FirstAsync(x => x.TytStoryId == StoryId);
        work.IsEarning.ShouldBeFalse();
        work.TytEarningNote!.ShouldContain("Chưa đủ điều kiện");
        // Truyện chờ duyệt không bị hỏi trang thưởng.
        _tyt.Verify(x => x.GetEarningAsync(It.IsAny<TytContext>(), It.Is<string>(id => id != StoryId), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnceEligible_TurnsEarningOn_AndCopiesTheFigures_ThenKeepsRefreshingThem()
    {
        _tyt.Setup(x => x.GetEarningAsync(It.IsAny<TytContext>(), StoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TytEarningDto(true, true, 1250, 300, 50, 900, null, 1000, 412));

        (await RunAsync()).ShouldBe(1);

        var work = await _db.Works.FirstAsync(x => x.TytStoryId == StoryId);
        work.IsEarning.ShouldBeTrue();
        (work.EarnCount, work.EarnNow, work.Paid, work.AdCount).ShouldBe((1250, 300, 900, 412));
        work.TytEarningSyncedAt.ShouldBe(Now);

        _tyt.Setup(x => x.GetEarningAsync(It.IsAny<TytContext>(), StoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TytEarningDto(true, true, 1500, 550, 0, 900, null, 1200, 500));
        await RunAsync();
        (await _db.Works.FirstAsync(x => x.TytStoryId == StoryId)).EarnNow.ShouldBe(550);
    }

    [Test]
    public async Task EligibleButUnreadableFigures_EnablesEarning_WithoutOverwritingWithZeros()
    {
        var work = await _db.Works.FirstAsync(x => x.TytStoryId == StoryId);
        work.EarnNow = 77;
        await _db.SaveChangesAsync();
        _tyt.Setup(x => x.GetEarningAsync(It.IsAny<TytContext>(), StoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TytEarningDto(true, true, null, null, null, null, null, null, null));

        await RunAsync();

        var saved = await _db.Works.FirstAsync(x => x.TytStoryId == StoryId);
        saved.IsEarning.ShouldBeTrue();
        saved.EarnNow.ShouldBe(77);
        saved.TytEarningNote!.ShouldContain("chưa đọc được");
    }

    [Test]
    public async Task WhenTytIsUnreachable_RecordsTheReasonAndChangesNothingElse()
    {
        _tyt.Setup(x => x.GetEarningAsync(It.IsAny<TytContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TytException("phiên hết hạn"));

        (await RunAsync()).ShouldBe(0);

        var work = await _db.Works.FirstAsync(x => x.TytStoryId == StoryId);
        work.IsEarning.ShouldBeFalse();
        work.TytEarningNote!.ShouldContain("phiên hết hạn");
    }

    [Test]
    public async Task EligibleButEarningOff_TurnsItOnAtTyt_AndPaidCountsMoneyAlreadyTransferred()
    {
        // Số thật khảo sát: dư 3,066 + đã chuyển 9,990 = tổng 13,056.
        _tyt.Setup(x => x.GetEarningAsync(It.IsAny<TytContext>(), StoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TytEarningDto(true, false, 13056, 3066, 0, 0, 9990, 13056, 2007));

        (await RunAsync()).ShouldBe(1);

        _tyt.Verify(x => x.SetEarningAsync(It.IsAny<TytContext>(), StoryId, true, It.IsAny<CancellationToken>()), Times.Once);
        var work = await _db.Works.FirstAsync(x => x.TytStoryId == StoryId);
        (work.IsEarning, work.EarnCount, work.EarnNow, work.Paid, work.AdCount).ShouldBe((true, 13056, 3066, 9990, 2007));
        work.TytEarningNote!.ShouldContain("Đã bật nhận thưởng");
    }

    [Test]
    public async Task WhenTurningEarningOnFails_ItStaysOffAndSaysWhy()
    {
        _tyt.Setup(x => x.GetEarningAsync(It.IsAny<TytContext>(), StoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TytEarningDto(true, false, 10, 5, 0, 0, 0, 10, 3));
        _tyt.Setup(x => x.SetEarningAsync(It.IsAny<TytContext>(), StoryId, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TytException("TYT từ chối"));

        (await RunAsync()).ShouldBe(0);

        var work = await _db.Works.FirstAsync(x => x.TytStoryId == StoryId);
        work.IsEarning.ShouldBeFalse();
        work.TytEarningNote!.ShouldContain("TYT từ chối");
    }
}
