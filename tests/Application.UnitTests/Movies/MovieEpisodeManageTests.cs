using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;
using AutoMapper;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models.TikTok;
using SurveillanceCameras.Application.Local.Movies.Commands.CleanupMovieEpisodes;
using SurveillanceCameras.Application.Local.Movies.Commands.DeleteMovieEpisodes;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;
using SurveillanceCameras.Application.Local.Movies.Commands.SyncTikTokDramas;
using SurveillanceCameras.Application.Local.Movies.Commands.UpdateMovieEpisode;
using SurveillanceCameras.Application.Local.Movies.Queries.DTOs;
using SurveillanceCameras.Application.Local.Movies.Queries.GetMovieEpisodes;
using SurveillanceCameras.Application.Repository.Media;
using SurveillanceCameras.Application.Repository.TikTok;
using SurveillanceCameras.Application.Repository.VideoStorage;
using SurveillanceCameras.Domain.Constants;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Data;

namespace SurveillanceCameras.Application.UnitTests.Movies;

/// <summary>Sửa, xoá, dọn tập phim lỗi/trùng, và đồng bộ lại không phá các thay đổi đó.</summary>
public class MovieEpisodeManageTests
{
    private const string Owner = "user-1";
    private const string DramaId = "7677108486182392852";

    private ApplicationDbContext _db = null!;
    private Mock<ITikTokRepository> _tikTok = null!;
    private Mock<IVideoStorageRepository> _storage = null!;
    private Mock<IUser> _user = null!;
    private Work _movie = null!;
    private UserProfile _channel = null!;

    [SetUp]
    public async Task SetUp()
    {
        _db = TestDb.Create();
        _tikTok = new Mock<ITikTokRepository>();
        _storage = new Mock<IVideoStorageRepository>();
        _user = new Mock<IUser>();
        _user.Setup(x => x.Id).Returns(Owner);
        _channel = new UserProfile
        {
            Title = "Kênh", UniqueId = "kenh", SecUid = "sec", CreatedBy = Owner,
            UserProfileType = new UserProfileType { Title = "TikTok", Code = UserProfileTypeCodes.TikTok, CreatedBy = Owner }
        };
        _db.UserProfiles.Add(_channel);
        await _db.SaveChangesAsync();
        _movie = new Work { Title = "Phim", Type = WorkType.Movie, CreatedBy = Owner, UserProfileId = _channel.Id, ExternalId = DramaId, Episodes = [] };
        _db.Works.Add(_movie);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private EpisodeVideoStorer Storer()
    {
        var protector = new Mock<ISecretProtector>();
        protector.Setup(x => x.Unprotect(It.IsAny<string>())).Returns<string>(x => x);
        return new EpisodeVideoStorer(_db, _tikTok.Object, _storage.Object, protector.Object,
            Options.Create(new EpisodeVideoStoreOptions()), new DramaFileCache(TimeProvider.System), NullLogger<EpisodeVideoStorer>.Instance);
    }

    private async Task<Episode> AddAsync(int number, int? duration = 60, string? externalId = null, string? videoUrl = "https://tiktok.com/v",
        string? stream = null, string? storageError = null, bool edited = false)
    {
        var episode = new Episode
        {
            WorkId = _movie.Id, EpisodeNumber = number, Title = $"Tập {number}", DurationSeconds = duration, CreatedBy = Owner,
            ExternalId = externalId ?? $"70{number}-{Guid.NewGuid():N}", VideoUrl = videoUrl, StreamUrl = stream,
            StorageError = storageError, IsEdited = edited
        };
        _db.Episodes.Add(episode);
        await _db.SaveChangesAsync();
        return episode;
    }

    private Task<CleanupMovieEpisodesResult> CleanupAsync(bool apply, bool removeBroken = true, bool renumber = false)
        => new CleanupMovieEpisodesCommandHandler(_db, _user.Object, Storer())
            .Handle(new CleanupMovieEpisodesCommand { MovieId = _movie.Id, Apply = apply, RemoveBroken = removeBroken, Renumber = renumber }, CancellationToken.None);

    [Test]
    public async Task Update_ChangesNumberAndName_MarksEdited_AndRefusesADuplicateNumber()
    {
        var first = await AddAsync(1);
        await AddAsync(2);
        var handler = new UpdateMovieEpisodeCommandHandler(_db, _user.Object);

        await handler.Handle(new UpdateMovieEpisodeCommand(_movie.Id, first.Id, 5, "  Tên mới "), CancellationToken.None);

        first.EpisodeNumber.ShouldBe(5);
        first.Title.ShouldBe("Tập 5");
        first.NameRaw.ShouldBe("Tên mới");
        first.IsEdited.ShouldBeTrue();
        await Should.ThrowAsync<ValidationException>(() =>
            handler.Handle(new UpdateMovieEpisodeCommand(_movie.Id, first.Id, 2, "x"), CancellationToken.None));
    }

    [Test]
    public async Task Delete_RemovesEpisodes_ReleasesStoredVideo_AndRemembersTheVideoId()
    {
        var account = new StorageAccount { Title = "a", CloudflareAccountId = "c", AccessKeyId = "k", Bucket = "b", SecretAccessKey = "s",
            PublicBaseUrl = "https://a.example.com", UsedBytes = 30_000_000, CreatedBy = Owner };
        _db.StorageAccounts.Add(account);
        await _db.SaveChangesAsync();
        var stored = await AddAsync(1, externalId: "vid-1", stream: "https://a.example.com/x.mp4");
        stored.StorageAccountId = account.Id;
        stored.StorageKey = "movies/x.mp4";
        stored.StorageSizeBytes = 30_000_000;
        var other = await AddAsync(2, externalId: "vid-2");
        await _db.SaveChangesAsync();

        var removed = await new DeleteMovieEpisodesCommandHandler(_db, _user.Object, Storer())
            .Handle(new DeleteMovieEpisodesCommand(_movie.Id, [stored.Id]), CancellationToken.None);

        removed.ShouldBe(1);
        _db.Episodes.Select(x => x.Id).ToList().ShouldBe([other.Id]);
        _storage.Verify(x => x.DeleteAsync(It.IsAny<VideoStorageTarget>(), "movies/x.mp4", It.IsAny<CancellationToken>()), Times.Once);
        account.UsedBytes.ShouldBe(0);
        (await _db.Works.FirstAsync()).ExcludedEpisodeIds.ShouldBe(["vid-1"]);
    }

    [Test]
    public async Task Cleanup_RemovesCopies_KeepingTheOneWithAFile_AndPreviewChangesNothing()
    {
        var plain = await AddAsync(3, duration: 60);
        var withFile = await AddAsync(3, duration: 61, stream: "https://a.example.com/3.mp4");
        await AddAsync(4);

        var preview = await CleanupAsync(apply: false);
        preview.Applied.ShouldBeFalse();
        preview.Duplicates.ShouldBe(1);
        _db.Episodes.Count().ShouldBe(3);

        var result = await CleanupAsync(apply: true);

        result.Duplicates.ShouldBe(1);
        _db.Episodes.Select(x => x.Id).ToList().ShouldNotContain(plain.Id);
        _db.Episodes.Select(x => x.Id).ToList().ShouldContain(withFile.Id);
        (await _db.Works.FirstAsync()).ExcludedEpisodeIds.ShouldContain(plain.ExternalId!);
    }

    [Test]
    public async Task Cleanup_ReportsAConflict_WhenSameNumberHasDifferentContent_AndRenumbersOnRequest()
    {
        await AddAsync(1, duration: 60);
        await AddAsync(2, duration: 60);
        await AddAsync(2, duration: 300);   // khác hẳn thời lượng: không phải bản sao
        await AddAsync(3, duration: 60);

        var report = await CleanupAsync(apply: true);
        report.Conflicts.ShouldBe(1);
        report.Duplicates.ShouldBe(0);
        _db.Episodes.Count().ShouldBe(4);

        var fixedUp = await CleanupAsync(apply: true, renumber: true);
        fixedUp.Renumbered.ShouldBe(2);
        _db.Episodes.OrderBy(x => x.Id).Select(x => x.EpisodeNumber).ToList().ShouldBe([1, 2, 3, 4]);
    }

    [Test]
    public async Task Cleanup_DropsEpisodesWithNoSourceAtAll_AndRetriesFailedStorage()
    {
        await AddAsync(1);
        var broken = await AddAsync(2, videoUrl: null);
        var failed = await AddAsync(3, storageError: "hết chỗ");
        failed.StorageAttempts = 3;
        await _db.SaveChangesAsync();

        var result = await CleanupAsync(apply: true);

        result.Broken.ShouldBe(1);
        result.StorageRetried.ShouldBe(1);
        _db.Episodes.Select(x => x.Id).ToList().ShouldNotContain(broken.Id);
        failed.StorageAttempts.ShouldBe(0);
        failed.StorageError.ShouldBeNull();
    }

    [Test]
    public async Task Query_FlagsDuplicatesAcrossPages_AndCanFilterToIssuesOnly()
    {
        await AddAsync(1);
        await AddAsync(2);
        await AddAsync(2, duration: 90);
        await AddAsync(3, videoUrl: null);
        await AddAsync(4, storageError: "lỗi");
        var mapper = new MapperConfiguration(c => c.AddMaps(typeof(MovieEpisodeDto).Assembly), NullLoggerFactory.Instance).CreateMapper();
        var handler = new GetMovieEpisodesQueryHandler(_db, _user.Object, mapper);

        var all = await handler.Handle(new GetMovieEpisodesQuery { MovieId = _movie.Id, PageSize = 2 }, CancellationToken.None);
        all.Items.Count.ShouldBe(2);   // trang 1 chỉ có tập 1 và một bản của tập 2
        all.Items.Count(x => x.IsDuplicate).ShouldBe(1);

        var issues = await handler.Handle(new GetMovieEpisodesQuery { MovieId = _movie.Id, OnlyIssues = true }, CancellationToken.None);
        issues.Items.Select(x => x.EpisodeNumber).Order().ToList().ShouldBe([2, 2, 3, 4]);
        issues.Items.Single(x => x.EpisodeNumber == 3).IsMissingSource.ShouldBeTrue();
    }

    private static DramaEpisodeDto Item(string id, int number, string desc = "Mô tả") => new()
    {
        Id = id, Desc = desc, Author = new DramaEpisodeAuthorDto { UniqueId = "kenh" },
        Video = new DramaEpisodeVideoDto { Duration = 60 },
        DramaInfo = new DramaEpisodeInfoDto { DramaName = "Phim", NumVideos = 5, DramaVideoData = new DramaVideoDataDto { EpisodeNumber = number } }
    };

    private async Task SyncAsync(params DramaEpisodeDto[] items)
    {
        _tikTok.Setup(x => x.FetchDramaEpisodes(null, DramaId, It.IsAny<int>(), "0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DramaEpisodeListDto { ItemList = [.. items], HasMore = false });
        var media = new Mock<IMediaRepository>();
        await new TikTokDramaSyncer(_db, _tikTok.Object, media.Object, TimeProvider.System, NullLogger<TikTokDramaSyncer>.Instance)
            .SyncAsync(Owner, _channel.Id, [DramaId], CancellationToken.None);
        _db.ChangeTracker.Clear();
    }

    [Test]
    public async Task Resync_KeepsEditedEpisodes_DoesNotBringBackDeletedOnes_AndAdoptsAReuploadedEpisode()
    {
        await AddAsync(1, externalId: "v1", edited: true);
        (await _db.Episodes.FirstAsync()).NameRaw = "Tên tôi đặt";
        var reuploaded = await AddAsync(2, externalId: "v2-cu", stream: "https://a.example.com/2.mp4");
        _movie.ExcludedEpisodeIds = ["v3"];
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        // TikTok trả: tập 1 (tên khác), tập 2 với ID mới, tập 3 đã xoá, tập 4 mới.
        await SyncAsync(Item("v1", 1, "Tên TikTok"), Item("v2-moi", 2), Item("v3", 3), Item("v4", 4));

        var episodes = await _db.Episodes.OrderBy(x => x.EpisodeNumber).ToListAsync();
        episodes.Select(x => x.EpisodeNumber).ShouldBe([1, 2, 4]);   // không có tập 3, không nhân đôi tập 2
        episodes[0].NameRaw.ShouldBe("Tên tôi đặt");
        episodes[1].Id.ShouldBe(reuploaded.Id);
        episodes[1].ExternalId.ShouldBe("v2-moi");
        episodes[1].StreamUrl.ShouldBe("https://a.example.com/2.mp4");   // giữ file đã lưu
    }
}
