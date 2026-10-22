using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models.TikTok;
using SurveillanceCameras.Application.Local.Movies.Commands.DeleteMovie;
using SurveillanceCameras.Application.Local.Movies.Commands.ResyncAllMovies;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;
using SurveillanceCameras.Application.Local.Movies.Commands.SyncChannelMovies;
using SurveillanceCameras.Application.Repository.VideoStorage;
using SurveillanceCameras.Application.Local.Movies.Commands.SyncMovieFromTikTok;
using SurveillanceCameras.Application.Local.Movies.Commands.SyncTikTokDramas;
using SurveillanceCameras.Application.Repository.Media;
using SurveillanceCameras.Application.Repository.TikTok;
using SurveillanceCameras.Domain.Constants;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Data;

namespace SurveillanceCameras.Application.UnitTests.Movies;

public class SyncTikTokDramasTests
{
    private const string Owner = "user-1";
    private const string DramaId = "7677108486182392852";

    private ApplicationDbContext _db = null!;
    private Mock<ITikTokRepository> _tikTok = null!;
    private Mock<IMediaRepository> _media = null!;
    private UserProfile _channel = null!;

    [SetUp]
    public async Task SetUp()
    {
        _db = TestDb.Create();
        _tikTok = new Mock<ITikTokRepository>();
        _media = new Mock<IMediaRepository>();
        _media.Setup(x => x.IsStored(It.IsAny<string>())).Returns<string?>(url => url?.StartsWith("/media/") == true);
        _media.Setup(x => x.SaveImageFromUrlAsync(It.IsAny<string>(), "covers/movies", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string folder, string name, CancellationToken _) => $"/media/{folder}/{name}.jpg");
        _channel = new UserProfile
        {
            Title = "StardustTV", UniqueId = "stardusttv_official", SecUid = "sec", CreatedBy = Owner,
            UserProfileType = new UserProfileType { Title = "TikTok", Code = UserProfileTypeCodes.TikTok, CreatedBy = Owner }
        };
        _db.UserProfiles.Add(_channel);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private static DramaEpisodeDto Ep(int number, int numVideos = 3, string? language = null) => new()
    {
        TextLanguage = language,
        Id = $"70000000000000000{number:00}",
        Desc = "Tycoon Returns",
        Author = new DramaEpisodeAuthorDto { UniqueId = "stardusttv_official" },
        Video = new DramaEpisodeVideoDto { Duration = 60 + number, Cover = $"https://img/{number}.jpg" },
        DramaInfo = new DramaEpisodeInfoDto
        {
            DramaName = "Tycoon Returns", Description = "Mô tả", NumVideos = numVideos,
            DramaVideoData = new DramaVideoDataDto { EpisodeNumber = number },
            ZoomCover = { ["480"] = "https://img/cover.jpg" }
        }
    };

    private void SetupPages(params DramaEpisodeListDto[] pages)
    {
        var cursor = "0";
        foreach (var page in pages)
        {
            var current = cursor;
            _tikTok.Setup(x => x.FetchDramaEpisodes(null, DramaId, It.IsAny<int>(), current, It.IsAny<CancellationToken>()))
                .ReturnsAsync(page);
            cursor = page.Cursor ?? cursor;
        }
    }

    private Task<SyncTikTokDramasResult> SyncAsync()
    {
        var user = new Mock<IUser>();
        user.Setup(x => x.Id).Returns(Owner);
        return new SyncTikTokDramasCommandHandler(_db, user.Object, Syncer())
            .Handle(new SyncTikTokDramasCommand { UserProfileId = _channel.Id, DramaIds = [DramaId] }, CancellationToken.None);
    }

    private TikTokDramaSyncer Syncer()
        => new(_db, _tikTok.Object, _media.Object, TimeProvider.System, NullLogger<TikTokDramaSyncer>.Instance);

    [Test]
    public async Task DailyResync_QueuesOneEventPerAutoSyncedTikTokChannel_WithoutCallingTikTok()
    {
        await FillChannelOwnerAsync();
        var off = new UserProfile { UserProfileTypeId = _channel.UserProfileTypeId, UniqueId = "tat", SecUid = "sec2", AutoSyncMovies = false, CreatedBy = Owner };
        var noSecUid = new UserProfile { UserProfileTypeId = _channel.UserProfileTypeId, UniqueId = "thieu", AutoSyncMovies = true, CreatedBy = Owner };
        _db.UserProfiles.AddRange(off, noSecUid);
        await _db.SaveChangesAsync();
        _tikTok.Invocations.Clear();

        var result = await new ResyncAllMoviesCommandHandler(_db, NullLogger<ResyncAllMoviesCommandHandler>.Instance)
            .Handle(new ResyncAllMoviesCommand(), CancellationToken.None);

        result.ShouldBe(new ResyncAllMoviesResult(Channels: 1, Queued: 1));
        _db.OutboxEventTypes().ShouldBe(["ChannelSyncRequestedIntegrationEvent"]);
        _tikTok.VerifyNoOtherCalls();
    }

    private async Task FillChannelOwnerAsync()
    {
        foreach (var profile in _db.UserProfiles.Where(x => x.CreatedBy == null)) profile.CreatedBy = Owner;
        await _db.SaveChangesAsync();
    }

    [Test]
    public async Task ChannelSync_QueuesNewAndExistingDramas_SkippingTheOnesTheUserDeleted()
    {
        SetupPages(new DramaEpisodeListDto { Cursor = "1", ItemList = [Ep(1, 2)] });
        await SyncAsync();
        await FillOwnerAsync();
        await FillChannelOwnerAsync();
        var channel = await _db.UserProfiles.FirstAsync(x => x.Id == _channel.Id);
        channel.ExcludedDramaIds = ["333"];
        await _db.SaveChangesAsync();
        _tikTok.Setup(x => x.FetchDramaList(null, It.IsAny<string>(), It.IsAny<int>(), "0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DramaListDto { HasMore = false, DramaList = [new DramaDto { DramaId = DramaId }, new DramaDto { DramaId = "222" }, new DramaDto { DramaId = "333" }] });

        var result = await new SyncChannelMoviesCommandHandler(_db, _tikTok.Object, TimeProvider.System,
                NullLogger<SyncChannelMoviesCommandHandler>.Instance)
            .Handle(new SyncChannelMoviesCommand(Owner, channel.Id, TimeSpan.Zero), CancellationToken.None);

        result.ShouldBe(new SyncChannelMoviesResult(Dramas: 3, Queued: 2, New: 1));
        _db.OutboxEventTypes().Count(x => x == "MovieSyncRequestedIntegrationEvent").ShouldBe(2);
        (await _db.UserProfiles.FirstAsync(x => x.Id == channel.Id)).MoviesSyncedAt.ShouldNotBeNull();
    }

    [Test]
    public async Task ChannelSync_WhenTikTokFails_RecordsTheErrorOnTheChannelAndQueuesNothing()
    {
        await FillChannelOwnerAsync();
        _tikTok.Setup(x => x.FetchDramaList(null, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TikTokBlockedException("trang thử thách"));

        var result = await new SyncChannelMoviesCommandHandler(_db, _tikTok.Object, TimeProvider.System,
                NullLogger<SyncChannelMoviesCommandHandler>.Instance)
            .Handle(new SyncChannelMoviesCommand(Owner, _channel.Id, TimeSpan.Zero), CancellationToken.None);

        result.Queued.ShouldBe(0);
        _db.OutboxEventTypes().ShouldBeEmpty();
        (await _db.UserProfiles.FirstAsync(x => x.Id == _channel.Id)).MoviesSyncError.ShouldBe("trang thử thách");
    }

    [Test]
    public async Task DeletingAMovie_RemembersTheDramaOnTheChannel()
    {
        SetupPages(new DramaEpisodeListDto { Cursor = "1", ItemList = [Ep(1, 2)] });
        await SyncAsync();
        await FillOwnerAsync();
        await FillChannelOwnerAsync();
        var movie = await _db.Works.SingleAsync(x => x.ExternalId == DramaId);
        var user = new Mock<IUser>();
        user.Setup(x => x.Id).Returns(Owner);

        await new DeleteMovieCommandHandler(_db, user.Object, new EpisodeVideoStorer(_db, _tikTok.Object, Mock.Of<IVideoStorageRepository>(),
                Mock.Of<ISecretProtector>(), Microsoft.Extensions.Options.Options.Create(new EpisodeVideoStoreOptions()),
                new DramaFileCache(TimeProvider.System), NullLogger<EpisodeVideoStorer>.Instance))
            .Handle(new DeleteMovieCommand(movie.Id), CancellationToken.None);

        (await _db.UserProfiles.FirstAsync(x => x.Id == _channel.Id)).ExcludedDramaIds.ShouldBe([DramaId]);
    }

    [Test]
    public async Task SyncMovieFromTikTok_UpdatesTheMovie_WithoutUser()
    {
        SetupPages(new DramaEpisodeListDto { Cursor = "1", ItemList = [Ep(1, 2)] });
        await SyncAsync();
        await FillOwnerAsync();

        SetupPages(new DramaEpisodeListDto { Cursor = "2", ItemList = [Ep(1, 2), Ep(2, 2)] });
        var result = await new SyncMovieFromTikTokCommandHandler(Syncer())
            .Handle(new SyncMovieFromTikTokCommand(Owner, _channel.Id, DramaId), CancellationToken.None);

        result.Updated.ShouldBe(1);
        result.EpisodesAdded.ShouldBe(1);
        var movie = await _db.Works.Include(x => x.Episodes).SingleAsync(x => x.ExternalId == DramaId);
        movie.Episodes!.Count.ShouldBe(2);
        movie.IsFull.ShouldBeTrue();
        movie.Episodes.ShouldAllBe(x => x.CreatedBy == Owner);
    }

    private async Task FillOwnerAsync()
    {
        // TestDb không có người dùng đăng nhập nên interceptor không điền CreatedBy như khi chạy thật.
        foreach (var work in _db.Works.Where(x => x.CreatedBy == null)) work.CreatedBy = Owner;
        await _db.SaveChangesAsync();
    }

    [Test]
    public async Task SyncsLanguageFromTextLanguage_ReusingDeclaredLanguageAndCreatingMissingOnes()
    {
        // Đã khai báo sẵn "EN" (khác hoa thường) và một ngôn ngữ cùng mã của người khác.
        var english = new Language { Title = "English", Code = "EN", CreatedBy = Owner };
        _db.Languages.AddRange(english, new Language { Title = "Của người khác", Code = "en", CreatedBy = "user-khác" });
        await _db.SaveChangesAsync();
        SetupPages(new DramaEpisodeListDto
        {
            Cursor = "1", ItemList = [Ep(1, 4, "en"), Ep(2, 4, "en"), Ep(3, 4, "es"), Ep(4, 4, "un")]
        });

        await SyncAsync();

        var movie = await _db.Works.Include(x => x.Language).Include(x => x.Episodes!).ThenInclude(x => x.Language).SingleAsync();
        var episodes = movie.Episodes!.OrderBy(x => x.EpisodeNumber).ToList();
        episodes.Select(x => x.Language?.Code).ShouldBe(["EN", "EN", "es", null]);
        episodes[0].LanguageId.ShouldBe(english.Id);
        // Phim lấy ngôn ngữ có nhiều tập nhất.
        movie.LanguageId.ShouldBe(english.Id);
        var mine = _db.Languages.Where(x => x.CreatedBy == Owner).ToList();
        mine.Count.ShouldBe(2);
        mine.Single(x => x.Code == "es").Title.ShouldBe("Tiếng Tây Ban Nha");

        // Đồng bộ lại: không tạo trùng; TikTok không trả ngôn ngữ thì giữ ngôn ngữ đã có.
        SetupPages(new DramaEpisodeListDto { Cursor = "1", ItemList = [Ep(1, 4), Ep(2, 4, "es"), Ep(3, 4, "es"), Ep(4, 4, "es")] });
        await SyncAsync();

        _db.Languages.Count(x => x.CreatedBy == Owner).ShouldBe(2);
        movie.Episodes!.OrderBy(x => x.EpisodeNumber).Select(x => x.Language?.Code).ShouldBe(["EN", "es", "es", "es"]);
        movie.Language!.Code.ShouldBe("es");
    }

    [Test]
    public async Task MovieLanguage_VietnameseWins_ElseMajority()
    {
        // Audio tiếng Anh + phụ đề tiếng Việt: TikTok nhận diện lẫn lộn, chỉ cần có tập "vi" là phim thuộc tiếng Việt.
        SetupPages(new DramaEpisodeListDto { Cursor = "1", ItemList = [Ep(1, 4, "en"), Ep(2, 4, "en"), Ep(3, 4, "en"), Ep(4, 4, "vi")] });
        await SyncAsync();
        var movie = await _db.Works.Include(x => x.Language).SingleAsync();
        movie.Language!.Code.ShouldBe("vi");

        // Không còn tập tiếng Việt: quay về ngôn ngữ nhiều tập nhất.
        SetupPages(new DramaEpisodeListDto { Cursor = "1", ItemList = [Ep(1, 4, "en"), Ep(2, 4, "en"), Ep(3, 4, "en"), Ep(4, 4, "es")] });
        await SyncAsync();
        (await _db.Works.Include(x => x.Language).SingleAsync()).Language!.Code.ShouldBe("en");
    }

    [Test]
    public async Task CreatesMovie_WithAllEpisodesAcrossPages()
    {
        SetupPages(
            new DramaEpisodeListDto { Cursor = "2", HasMore = true, ItemList = [Ep(1), Ep(2)] },
            new DramaEpisodeListDto { Cursor = "3", HasMore = false, ItemList = [Ep(3)] });

        var result = await SyncAsync();

        result.Created.ShouldBe(1);
        result.EpisodesAdded.ShouldBe(3);
        var movie = await _db.Works.Include(x => x.Episodes).SingleAsync();
        movie.Type.ShouldBe(WorkType.Movie);
        movie.Title.ShouldBe("Tycoon Returns");
        movie.CoverUrl.ShouldBe($"/media/covers/movies/{movie.Id}.jpg");
        movie.UserProfileId.ShouldBe(_channel.Id);
        movie.ExternalId.ShouldBe(DramaId);
        movie.IsFull.ShouldBeTrue();
        var episodes = movie.Episodes!.OrderBy(x => x.EpisodeNumber).ToList();
        episodes.Select(x => x.EpisodeNumber).ShouldBe([1, 2, 3]);
        episodes[0].VideoUrl.ShouldBe("https://www.tiktok.com/@stardusttv_official/video/7000000000000000001");
        episodes[0].DurationSeconds.ShouldBe(61);
        episodes[0].Title.ShouldBe("Tập 1");
    }

    [Test]
    public async Task SyncAgain_UpdatesMovie_AndAddsOnlyNewEpisodes()
    {
        SetupPages(new DramaEpisodeListDto { Cursor = "2", ItemList = [Ep(1, 5), Ep(2, 5)] });
        await SyncAsync();
        await FillOwnerAsync();

        SetupPages(new DramaEpisodeListDto { Cursor = "3", ItemList = [Ep(1, 5), Ep(2, 5), Ep(3, 5)] });
        var result = await SyncAsync();

        result.Updated.ShouldBe(1);
        result.Created.ShouldBe(0);
        result.EpisodesAdded.ShouldBe(1);
        (await _db.Works.CountAsync()).ShouldBe(1);
        (await _db.Episodes.CountAsync()).ShouldBe(3);
        (await _db.Works.SingleAsync()).IsFull.ShouldBeFalse();   // 3/5 tập
    }

    [Test]
    public async Task StoredCover_IsNotDownloadedAgainOnResync()
    {
        SetupPages(new DramaEpisodeListDto { Cursor = "1", ItemList = [Ep(1)] });
        await SyncAsync();
        await FillOwnerAsync();
        await SyncAsync();

        _media.Verify(x => x.SaveImageFromUrlAsync("https://img/cover.jpg", "covers/movies", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        (await _db.Works.SingleAsync()).CoverUrl!.ShouldStartWith("/media/");
    }

    [Test]
    public async Task CoverDownloadFails_KeepsTikTokLink()
    {
        _media.Setup(x => x.SaveImageFromUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MediaException("HTTP 403"));
        SetupPages(new DramaEpisodeListDto { Cursor = "1", ItemList = [Ep(1)] });

        var result = await SyncAsync();

        result.Created.ShouldBe(1);
        (await _db.Works.SingleAsync()).CoverUrl.ShouldBe("https://img/cover.jpg");
    }

    [Test]
    public async Task TikTokError_IsReported_NotThrown()
    {
        _tikTok.Setup(x => x.FetchDramaEpisodes(null, DramaId, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TikTokApiException("TikTok trả về HTTP 403"));

        var result = await SyncAsync();

        result.Errors.Single().ShouldContain("403");
        (await _db.Works.CountAsync()).ShouldBe(0);
    }

    [Test]
    public void ChannelOfOtherUser_IsNotFound()
    {
        _channel.CreatedBy = "user-khác";
        _db.SaveChanges();

        Should.Throw<Ardalis.GuardClauses.NotFoundException>(SyncAsync);
    }
}
