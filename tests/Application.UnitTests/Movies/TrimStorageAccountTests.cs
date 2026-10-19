using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;
using SurveillanceCameras.Application.Local.StorageAccounts.Commands.TrimStorageAccount;
using SurveillanceCameras.Application.Local.StorageAccounts.Commands.UpdateStorageAccount;
using SurveillanceCameras.Application.Repository.TikTok;
using SurveillanceCameras.Application.Repository.VideoStorage;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Data;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace SurveillanceCameras.Application.UnitTests.Movies;

public class TrimStorageAccountTests
{
    private const string Owner = "user-1";
    private const long Mb = StorageAccount.BytesPerMb;

    private ApplicationDbContext _db = null!;
    private Mock<IVideoStorageRepository> _storage = null!;
    private Dictionary<string, long> _bucket = null!;
    private StorageAccount _account = null!;
    private Work _popular = null!;
    private Work _minor = null!;

    [SetUp]
    public async Task SetUp()
    {
        _db = TestDb.Create();
        _bucket = [];
        _storage = new Mock<IVideoStorageRepository>();
        _storage.Setup(x => x.ListAsync(It.IsAny<VideoStorageTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _bucket.Select(x => new VideoStorageObject(x.Key, x.Value)).ToList());
        _storage.Setup(x => x.DeleteAsync(It.IsAny<VideoStorageTarget>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((VideoStorageTarget _, string key, CancellationToken _) => { _bucket.Remove(key); return Task.CompletedTask; });

        _account = new StorageAccount
        {
            Title = "R2", CloudflareAccountId = "acc", AccessKeyId = "key", SecretAccessKey = "secret", Bucket = "movie-01",
            PublicBaseUrl = "https://pub.example.com", QuotaBytes = 500 * Mb, CreatedBy = Owner
        };
        _db.StorageAccounts.Add(_account);
        await _db.SaveChangesAsync();

        _popular = Movie("Nhiều lượt xem", 9_000_000, 3);
        _minor = Movie("Ít lượt xem", 1_000, 3);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    /// <summary>Phim có các tập đã lưu vào kho, mỗi tập 100 MB.</summary>
    private Work Movie(string title, long views, int episodes)
    {
        var movie = new Work { Title = title, Type = WorkType.Movie, ViewCount = views, CreatedBy = Owner, Episodes = [] };
        _db.Works.Add(movie);
        for (var number = 1; number <= episodes; number++)
        {
            var key = $"movies/{title}/{number}.mp4";
            _bucket[key] = 100 * Mb;
            movie.Episodes.Add(new Episode
            {
                EpisodeNumber = number, StorageAccountId = _account.Id, StorageKey = key, StorageSizeBytes = 100 * Mb,
                StreamUrl = $"https://pub.example.com/{key}", VideoUrl = "https://www.tiktok.com/video/1", CreatedBy = Owner
            });
        }

        return movie;
    }

    private Task<TrimStorageAccountResult> TrimAsync(bool apply, bool deleteOrphans = false, string user = Owner)
    {
        var currentUser = new Mock<IUser>();
        currentUser.Setup(x => x.Id).Returns(user);
        var protector = new Mock<ISecretProtector>();
        protector.Setup(x => x.Unprotect(It.IsAny<string>())).Returns<string>(x => x);
        var options = MsOptions.Create(new EpisodeVideoStoreOptions { MaxAttempts = 3 });
        var storer = new EpisodeVideoStorer(_db, Mock.Of<ITikTokRepository>(), _storage.Object, protector.Object, options,
            NullLogger<EpisodeVideoStorer>.Instance);

        return new TrimStorageAccountCommandHandler(_db, currentUser.Object, protector.Object, _storage.Object, storer,
                TimeProvider.System, options)
            .Handle(new TrimStorageAccountCommand(_account.Id, apply, deleteOrphans), CancellationToken.None);
    }

    [Test]
    public void DefaultQuota_IsTenDecimalGigabytes()
    {
        // Cloudflare tính 1 GB = 1 tỷ byte; 1024³ sẽ cho phép lưu tới 10,74 GB theo cách Cloudflare đo.
        StorageAccount.DefaultQuotaBytes.ShouldBe(10_000_000_000);
    }

    [Test]
    public async Task DryRun_PlansReleasingLeastViewedMovieLastEpisodesFirst_WithoutDeleting()
    {
        // 600 MB trên hạn mức 500 MB: phải gỡ 1 tập.
        var plan = await TrimAsync(apply: false);

        plan.Applied.ShouldBeFalse();
        plan.UsedBytesBefore.ShouldBe(600 * Mb);
        plan.UsedBytesAfter.ShouldBe(500 * Mb);
        plan.WithinQuota.ShouldBeTrue();
        plan.EpisodesReleased.ShouldBe(1);
        plan.Movies.ShouldBe([new TrimmedMovie(_minor.Id, "Ít lượt xem", 1, 3, 3, 100 * Mb)]);
        _bucket.Count.ShouldBe(6);
        _db.Episodes.Count(x => x.StreamUrl != null).ShouldBe(6);
        _storage.Verify(x => x.DeleteAsync(It.IsAny<VideoStorageTarget>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Apply_DeletesFilesUnlinksEpisodesAndReconcilesUsage()
    {
        _account.QuotaBytes = 250 * Mb; // phải gỡ 4 tập: cả phim ít xem + tập cuối của phim nhiều xem
        _account.UsedBytes = 123;       // số cộng dồn sai lệch từ trước
        await _db.SaveChangesAsync();

        var result = await TrimAsync(apply: true);

        result.EpisodesReleased.ShouldBe(4);
        result.UsedBytesAfter.ShouldBe(200 * Mb);
        _bucket.Keys.ShouldBe(["movies/Nhiều lượt xem/1.mp4", "movies/Nhiều lượt xem/2.mp4"], ignoreOrder: true);
        _account.UsedBytes.ShouldBe(200 * Mb);
        var released = _db.Episodes.Where(x => x.StreamUrl == null).ToList();
        released.Count.ShouldBe(4);
        released.ShouldAllBe(x => x.StorageKey == null && x.StorageAccountId == null && x.StorageError != null && x.StorageAttempts == 3);
        // Tập đầu của phim nhiều lượt xem vẫn phát được.
        _popular.Episodes!.Where(x => x.EpisodeNumber <= 2).ShouldAllBe(x => x.StreamUrl != null);
    }

    [Test]
    public async Task Orphans_AreCountedAlways_ButDeletedOnlyWhenAsked_AndForeignFilesAreNeverTouched()
    {
        _bucket["movies/9/mo-coi.mp4"] = 150 * Mb; // file mồ côi của hệ thống
        _bucket["backup/khac.zip"] = 50 * Mb;      // file người dùng tự để vào bucket

        // 800 MB / 500 MB. Không xoá mồ côi: phải gỡ 3 tập.
        var keep = await TrimAsync(apply: false);
        (keep.OrphanObjects, keep.OrphanBytes, keep.EpisodesReleased).ShouldBe((1, 150 * Mb, 3));

        // Xoá mồ côi trước thì chỉ còn phải gỡ 2 tập.
        var result = await TrimAsync(apply: true, deleteOrphans: true);
        result.EpisodesReleased.ShouldBe(2);
        result.UsedBytesAfter.ShouldBe(450 * Mb);
        _bucket.ContainsKey("movies/9/mo-coi.mp4").ShouldBeFalse();
        _bucket.ContainsKey("backup/khac.zip").ShouldBeTrue();
    }

    [Test]
    public async Task WithinQuota_ChangesNothing_AndOtherUsersCannotTrim()
    {
        _account.QuotaBytes = 10_000 * Mb;
        await _db.SaveChangesAsync();

        var result = await TrimAsync(apply: true);
        result.EpisodesReleased.ShouldBe(0);
        _bucket.Count.ShouldBe(6);

        await Should.ThrowAsync<Ardalis.GuardClauses.NotFoundException>(() => TrimAsync(apply: true, user: "người-khác"));
    }

    [Test]
    public async Task UnreadableBucket_FailsBeforeDeletingAnything()
    {
        _storage.Setup(x => x.ListAsync(It.IsAny<VideoStorageTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VideoStorageException("R2 từ chối"));

        await Should.ThrowAsync<VideoStorageException>(() => TrimAsync(apply: true));
        _db.Episodes.Count(x => x.StreamUrl != null).ShouldBe(6);
    }

    private Task UpdateAsync(string bucket, string publicBaseUrl)
    {
        var currentUser = new Mock<IUser>();
        currentUser.Setup(x => x.Id).Returns(Owner);
        var handler = new UpdateStorageAccountCommandHandler(_db, currentUser.Object, new Mock<ISecretProtector>().Object);
        return handler.Handle(new UpdateStorageAccountCommand
        {
            Id = _account.Id, Title = "R2", CloudflareAccountId = "acc", AccessKeyId = "key", Bucket = bucket,
            PublicBaseUrl = publicBaseUrl, QuotaBytes = 500 * Mb, IsEnabled = true
        }, CancellationToken.None);
    }

    [Test]
    public async Task SwitchingToCustomDomainRewritesStreamLinksButMovingBucketIsRefused()
    {
        await UpdateAsync("movie-01", "https://video.example.com/");

        var episode = _db.Episodes.First(x => x.StorageKey == "movies/Ít lượt xem/1.mp4");
        episode.StreamUrl.ShouldBe("https://video.example.com/movies/Ít lượt xem/1.mp4");
        _db.Episodes.Count(x => x.StreamUrl!.StartsWith("https://video.example.com/")).ShouldBe(6);

        await Should.ThrowAsync<ValidationException>(() => UpdateAsync("movie-02", "https://video.example.com"));
    }
}
