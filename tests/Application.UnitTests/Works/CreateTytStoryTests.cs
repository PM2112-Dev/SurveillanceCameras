using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Local.Works.Commands.CreateTytStory;
using SurveillanceCameras.Application.Repository.Tyt;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Infrastructure.Data;

namespace SurveillanceCameras.Application.UnitTests.Works;

public class CreateTytStoryTests
{
    private const string Owner = "user-1";
    private const string NewId = "69e4f10e09e2347bc852c468";

    private ApplicationDbContext _db = null!;
    private Mock<ITytRepository> _tyt = null!;
    private Account _account = null!;

    [SetUp]
    public async Task SetUp()
    {
        _db = TestDb.Create();
        _tyt = new Mock<ITytRepository>();

        _account = new Account
        {
            Title = "TYT chính", CreatedBy = Owner,
            AccountType = new AccountType { Title = "TYT", BaseUrl = "https://tytnovel.info/", CreatedBy = Owner }
        };
        _db.Accounts.Add(_account);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private CreateTytStoryCommandHandler Handler() => new(_db, _tyt.Object, TimeProvider.System, NullLogger<CreateTytStoryCommandHandler>.Instance);

    private async Task<Work> SeedStoryAsync(int? accountId = -1, string title = "Kiều Loan Truyện", string? tytId = null)
    {
        var story = new Work
        {
            Title = title, Author = "Xán Diêu", Description = "Đoạn 1\nĐoạn 2", IsFull = true, StorySourceId = 1, TytStoryId = tytId,
            AccountId = accountId == -1 ? _account.Id : accountId, CreatedBy = Owner
        };
        _db.Works.Add(story);
        await _db.SaveChangesAsync();
        return story;
    }

    [Test]
    public async Task CreatesOnTyt_WithAccountBaseUrl_AndSavesId()
    {
        var story = await SeedStoryAsync();
        _tyt.Setup(x => x.CreateStoryAsync(It.IsAny<TytContext>(), It.IsAny<TytCreateStoryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewId);

        await Handler().Handle(new CreateTytStoryCommand(story.Id), CancellationToken.None);

        var saved = await _db.Works.SingleAsync();
        saved.TytStoryId.ShouldBe(NewId);
        saved.TytError.ShouldBeNull();
        _tyt.Verify(x => x.CreateStoryAsync(
            It.Is<TytContext>(c => c.AccountId == _account.Id && c.BaseUrl == "https://tytnovel.info/"),
            It.Is<TytCreateStoryRequest>(r => r.Title == "Kiều Loan Truyện" && r.Author == "Xán Diêu"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UnlinkedStoryWithSameTitleOnTyt_IsReused_NotDuplicated()
    {
        var story = await SeedStoryAsync();
        _tyt.Setup(x => x.GetStoryIdAsync(It.IsAny<TytContext>(), "Kiều Loan Truyện", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewId);

        await Handler().Handle(new CreateTytStoryCommand(story.Id), CancellationToken.None);

        (await _db.Works.SingleAsync()).TytStoryId.ShouldBe(NewId);
        _tyt.Verify(x => x.CreateStoryAsync(It.IsAny<TytContext>(), It.IsAny<TytCreateStoryRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SameTitleAlreadyLinkedToAnotherStory_CreatesNew()
    {
        await SeedStoryAsync(tytId: "69e495b5b709112fc40d96f2");
        var story = await SeedStoryAsync();
        _tyt.Setup(x => x.GetStoryIdAsync(It.IsAny<TytContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("69e495b5b709112fc40d96f2");
        _tyt.Setup(x => x.CreateStoryAsync(It.IsAny<TytContext>(), It.IsAny<TytCreateStoryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewId);

        await Handler().Handle(new CreateTytStoryCommand(story.Id), CancellationToken.None);

        (await _db.Works.SingleAsync(x => x.Id == story.Id)).TytStoryId.ShouldBe(NewId);
    }

    [Test]
    public async Task TytError_IsSavedOnStory_NotThrown()
    {
        var story = await SeedStoryAsync();
        _tyt.Setup(x => x.GetStoryIdAsync(It.IsAny<TytContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TytException("Tài khoản chưa đăng nhập TYT"));

        await Handler().Handle(new CreateTytStoryCommand(story.Id), CancellationToken.None);

        var saved = await _db.Works.SingleAsync();
        saved.TytStoryId.ShouldBeNull();
        saved.TytError!.ShouldContain("chưa đăng nhập");
    }

    [Test]
    public async Task NoAccount_SavesError_WithoutCallingTyt()
    {
        var story = await SeedStoryAsync(accountId: null);

        await Handler().Handle(new CreateTytStoryCommand(story.Id), CancellationToken.None);

        (await _db.Works.SingleAsync()).TytError!.ShouldContain("tài khoản TYT");
        _tyt.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AfterCreate_UpdatesInfoOnTyt()
    {
        var story = await SeedStoryAsync();
        _tyt.Setup(x => x.CreateStoryAsync(It.IsAny<TytContext>(), It.IsAny<TytCreateStoryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewId);

        await Handler().Handle(new CreateTytStoryCommand(story.Id), CancellationToken.None);

        _tyt.Verify(x => x.UpdateStoryAsync(It.IsAny<TytContext>(), NewId,
            It.Is<TytUpdateStoryRequest>(r => r.Title == "Kiều Loan Truyện" && r.Author == "Xán Diêu"
                                               && r.Description == "Đoạn 1\nĐoạn 2" && r.Status == TytStoryStatus.Full
                                               && r.Type == TytStoryType.Translated),
            It.IsAny<CancellationToken>()), Times.Once);
        (await _db.Works.SingleAsync()).TytUpdatedAt.ShouldNotBeNull();
    }

    [Test]
    public async Task SourceWithoutEdit_IsPostedAsConvert()
    {
        var webSource = new WebSource { Title = "Wikidich", BaseUrl = "https://truyen.test", IsNeedEdit = false, CreatedBy = Owner };
        _db.WebSources.Add(webSource);
        await _db.SaveChangesAsync();
        var source = new StorySource { Title = "Nguồn", WebSourceId = webSource.Id, CreatedBy = Owner };
        _db.StorySources.Add(source);
        await _db.SaveChangesAsync();
        var story = await SeedStoryAsync(tytId: NewId);
        story.StorySourceId = source.Id;
        await _db.SaveChangesAsync();

        await Handler().Handle(new CreateTytStoryCommand(story.Id), CancellationToken.None);

        _tyt.Verify(x => x.UpdateStoryAsync(It.IsAny<TytContext>(), NewId,
            It.Is<TytUpdateStoryRequest>(r => r.Type == TytStoryType.Convert), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateFails_KeepsTytId_SoRetryDoesNotCreateAgain()
    {
        var story = await SeedStoryAsync();
        _tyt.Setup(x => x.CreateStoryAsync(It.IsAny<TytContext>(), It.IsAny<TytCreateStoryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewId);
        _tyt.Setup(x => x.UpdateStoryAsync(It.IsAny<TytContext>(), It.IsAny<string>(), It.IsAny<TytUpdateStoryRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TytException("Tên truyện đã tồn tại"));

        await Handler().Handle(new CreateTytStoryCommand(story.Id), CancellationToken.None);

        var saved = await _db.Works.SingleAsync();
        saved.TytStoryId.ShouldBe(NewId);
        saved.TytError!.ShouldContain("cập nhật thông tin lỗi");
        saved.TytUpdatedAt.ShouldBeNull();

        // Chạy lại: chỉ cập nhật, không tạo thêm truyện.
        await Handler().Handle(new CreateTytStoryCommand(story.Id), CancellationToken.None);
        _tyt.Verify(x => x.CreateStoryAsync(It.IsAny<TytContext>(), It.IsAny<TytCreateStoryRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task<Work> SeedStoryWithSourceAsync()
    {
        var source = new StorySource
        {
            Title = "Nguồn", WebSourceId = 99, ImageUrl = "https://truyen.test/a.jpg", CreatedBy = Owner,
            Categories = { new Category { Title = "Ngôn Tình", CreatedBy = Owner }, new Category { Title = "Cổ Đại", CreatedBy = Owner } }
        };
        _db.StorySources.Add(source);
        await _db.SaveChangesAsync();
        var story = await SeedStoryAsync(tytId: NewId);
        story.StorySourceId = source.Id;
        await _db.SaveChangesAsync();
        return story;
    }

    [Test]
    public async Task SendsCoverAndCategoriesOfStorySource()
    {
        var story = await SeedStoryWithSourceAsync();

        await Handler().Handle(new CreateTytStoryCommand(story.Id), CancellationToken.None);

        _tyt.Verify(x => x.UpdateStoryAsync(It.IsAny<TytContext>(), NewId,
            It.Is<TytUpdateStoryRequest>(r => r.CoverUrl == "https://truyen.test/a.jpg"
                                              && r.Genres!.OrderBy(g => g).SequenceEqual(new[] { "Cổ Đại", "Ngôn Tình" })),
            It.IsAny<CancellationToken>()), Times.Once);
        (await _db.Works.SingleAsync()).TytError.ShouldBeNull();
    }

    [Test]
    public async Task TytRejectsCoverOrGenres_UpdatesRestWithoutThem_AndWarns()
    {
        var story = await SeedStoryWithSourceAsync();
        _tyt.Setup(x => x.UpdateStoryAsync(It.IsAny<TytContext>(), It.IsAny<string>(),
                It.Is<TytUpdateStoryRequest>(r => r.CoverUrl != null), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TytException("Ảnh bìa không hợp lệ"));

        await Handler().Handle(new CreateTytStoryCommand(story.Id), CancellationToken.None);

        _tyt.Verify(x => x.UpdateStoryAsync(It.IsAny<TytContext>(), NewId,
            It.Is<TytUpdateStoryRequest>(r => r.CoverUrl == null && r.Genres == null && r.Description == "Đoạn 1\nĐoạn 2"),
            It.IsAny<CancellationToken>()), Times.Once);
        var saved = await _db.Works.SingleAsync();
        saved.TytUpdatedAt.ShouldNotBeNull();
        saved.TytError!.ShouldContain("Ảnh bìa không hợp lệ");
    }

    [Test]
    public async Task AlreadyOnTyt_OnlyUpdatesInfo()
    {
        var story = await SeedStoryAsync(tytId: NewId);

        await Handler().Handle(new CreateTytStoryCommand(story.Id), CancellationToken.None);

        _tyt.Verify(x => x.UpdateStoryAsync(It.IsAny<TytContext>(), NewId, It.IsAny<TytUpdateStoryRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _tyt.VerifyNoOtherCalls();
    }
}
