using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.CrawlStories.Commands.SyncCrawledStories;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.DTOs;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Infrastructure.Data;

namespace SurveillanceCameras.Application.UnitTests.CrawlStories;

public class SyncCrawledStoriesCategoryTests
{
    private const string Owner = "user-1";
    private const string StoryUrl = "https://truyen.test/truyen-a";

    private ApplicationDbContext _db = null!;
    private Mock<ICrawlStoryRepository> _crawl = null!;
    private WebSource _webSource = null!;

    [SetUp]
    public async Task SetUp()
    {
        _db = TestDb.Create();
        _crawl = new Mock<ICrawlStoryRepository>();
        _webSource = new WebSource
        {
            Title = "Nguồn", BaseUrl = "https://truyen.test/danh-sach", CreatedBy = Owner,
            CrawlConfig = """{ "story": { "title": "h1" } }"""
        };
        _db.WebSources.Add(_webSource);
        _db.Categories.AddRange(
            new Category { Title = "Ngôn Tình", CreatedBy = Owner },
            new Category { Title = "Cổ  Đại", CreatedBy = Owner },
            new Category { Title = "Đam Mỹ", CreatedBy = "user-khác" });
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task SyncAsync(params string[] genres)
    {
        _crawl.Setup(x => x.FetchStoryAsync(It.IsAny<CrawlContext>(), StoryUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoryDataDto { Title = "Truyện A", CoverUrl = "https://truyen.test/a.jpg", Genres = genres.ToList() });

        var factory = new Mock<ICrawlStoryRepositoryFactory>();
        factory.Setup(x => x.GetRepository(It.IsAny<WebSource>())).Returns(_crawl.Object);
        var user = new Mock<IUser>();
        user.Setup(x => x.Id).Returns(Owner);

        await new SyncCrawledStoriesCommandHandler(_db, user.Object, factory.Object).Handle(new SyncCrawledStoriesCommand
        {
            WebSourceId = _webSource.Id,
            Stories = [new SyncCrawledStoryItem { Link = StoryUrl, Title = "Truyện A" }]
        }, CancellationToken.None);

        // TestDb không có người dùng đăng nhập nên interceptor không điền CreatedBy như khi chạy thật.
        foreach (var source in _db.StorySources.Where(x => x.CreatedBy == null)) source.CreatedBy = Owner;
        await _db.SaveChangesAsync();
    }

    private Task<StorySource> SavedSourceAsync()
        => _db.StorySources.Include(x => x.Categories).SingleAsync();

    [Test]
    public async Task MatchingGenres_AreAssigned_OthersIgnored_NoNewCategory()
    {
        await SyncAsync("ngôn tình", "Cổ Đại ", "Xuyên Không", "Đam Mỹ");

        var source = await SavedSourceAsync();
        source.Categories.Select(x => x.Title).OrderBy(x => x).ShouldBe(["Cổ  Đại", "Ngôn Tình"]);
        source.ImageUrl.ShouldBe("https://truyen.test/a.jpg");
        (await _db.Categories.CountAsync()).ShouldBe(3);   // "Xuyên Không" không được tạo
    }

    [Test]
    public async Task SyncAgain_DoesNotDuplicate_AndKeepsManualCategories()
    {
        await SyncAsync("Ngôn Tình");
        await SyncAsync("Ngôn Tình", "Cổ Đại");

        var source = await SavedSourceAsync();
        source.Categories.Count.ShouldBe(2);

        await SyncAsync();   // trang nguồn không còn thể loại: không xoá Category đã gán
        (await SavedSourceAsync()).Categories.Count.ShouldBe(2);
    }
}
