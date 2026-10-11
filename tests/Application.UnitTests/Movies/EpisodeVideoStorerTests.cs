using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;
using SurveillanceCameras.Application.Repository.TikTok;
using SurveillanceCameras.Application.Repository.VideoStorage;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Data;
using SurveillanceCameras.Infrastructure.Security;

namespace SurveillanceCameras.Application.UnitTests.Movies;

public class EpisodeVideoStorerTests
{
    private const string Owner = "user-1";
    private const long Mb = 1024 * 1024;

    private ApplicationDbContext _db = null!;
    private Mock<ITikTokRepository> _tikTok = null!;
    private Mock<IVideoStorageRepository> _storage = null!;
    private AesSecretProtector _protector = null!;
    private EpisodeVideoStorer _storer = null!;
    private Work _movie = null!;
    private long _videoSize;

    [SetUp]
    public async Task SetUp()
    {
        _db = TestDb.Create();
        _protector = new AesSecretProtector("khoá-thử-nghiệm-đủ-dài-0123456789");
        _videoSize = 30 * Mb;
        _tikTok = new Mock<ITikTokRepository>();
        _tikTok.Setup(x => x.DownloadVideoAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _videoSize);
        _storage = new Mock<IVideoStorageRepository>();
        _storer = new EpisodeVideoStorer(_db, _tikTok.Object, _storage.Object, _protector,
            Options.Create(new EpisodeVideoStoreOptions { DelayBetweenVideosSeconds = 0, MaxAttempts = 3 }),
            NullLogger<EpisodeVideoStorer>.Instance);

        _movie = new Work { Title = "Phim", Type = WorkType.Movie, CreatedBy = Owner, Episodes = [] };
        _db.Works.Add(_movie);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<StorageAccount> AddAccount(string title, int priority, long quota, long used = 0, bool enabled = true,
        string owner = Owner)
    {
        var account = new StorageAccount
        {
            Title = title, CloudflareAccountId = "acc" + priority, AccessKeyId = "key", Bucket = "videos",
            SecretAccessKey = _protector.Protect("secret-" + title), PublicBaseUrl = $"https://{title}.example.com",
            QuotaBytes = quota, UsedBytes = used, Priority = priority, IsEnabled = enabled, CreatedBy = owner
        };
        _db.StorageAccounts.Add(account);
        await _db.SaveChangesAsync();
        return account;
    }

    private async Task<Episode> AddEpisode(int number)
    {
        var episode = new Episode
        {
            WorkId = _movie.Id, EpisodeNumber = number, ExternalId = "70" + number, CreatedBy = Owner,
            VideoUrl = $"https://www.tiktok.com/@kenh/video/70{number}"
        };
        _db.Episodes.Add(episode);
        await _db.SaveChangesAsync();
        return episode;
    }

    [Test]
    public async Task Store_UploadsToHighestPriorityAccount_AndRecordsUsage()
    {
        var second = await AddAccount("b", priority: 2, quota: 100 * Mb);
        var first = await AddAccount("a", priority: 1, quota: 100 * Mb, used: 10 * Mb);
        var episode = await AddEpisode(1);

        (await _storer.StoreNextBatchAsync(CancellationToken.None)).ShouldBe(1);

        episode.StorageAccountId.ShouldBe(first.Id);
        episode.StorageSizeBytes.ShouldBe(30 * Mb);
        episode.StorageKey.ShouldNotBeNull().ShouldStartWith($"movies/{_movie.Id}/{episode.Id}-");
        episode.StreamUrl.ShouldBe($"https://a.example.com/{episode.StorageKey}");
        episode.StorageError.ShouldBeNull();
        first.UsedBytes.ShouldBe(40 * Mb);
        second.UsedBytes.ShouldBe(0);
        // Khoá bí mật được giải mã trước khi gọi kho.
        _storage.Verify(x => x.UploadFileAsync(It.Is<VideoStorageTarget>(t => t.SecretAccessKey == "secret-a" && t.Bucket == "videos"),
            episode.StorageKey!, It.IsAny<string>(), "video/mp4", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Store_DownloadsWithTheOwnersLatestTikTokCookie()
    {
        await AddAccount("a", priority: 1, quota: 100 * Mb);
        await AddEpisode(1);
        var tikTok = new AccountType { Title = "TikTok", BaseUrl = "https://www.tiktok.com/login", CreatedBy = Owner };
        _db.Accounts.AddRange(
            new Account { Title = "cũ", AccountType = tikTok, Cookie = "sessionid=cu", CreatedBy = Owner },
            new Account { Title = "người khác", AccountType = tikTok, Cookie = "sessionid=khac", CreatedBy = "user-2" },
            new Account { Title = "trang truyện", Cookie = "sid=truyen", CreatedBy = Owner,
                AccountType = new AccountType { Title = "Truyện", BaseUrl = "https://truyen.example.com", CreatedBy = Owner } });
        await _db.SaveChangesAsync();
        var latest = new Account { Title = "mới", AccountType = tikTok, Cookie = "sessionid=moi", CreatedBy = Owner };
        _db.Accounts.Add(latest);
        await _db.SaveChangesAsync();
        latest.LastModified = DateTimeOffset.UtcNow.AddMinutes(5);
        await _db.SaveChangesAsync();

        await _storer.StoreNextBatchAsync(CancellationToken.None);

        _tikTok.Verify(x => x.DownloadVideoAsync("sessionid=moi", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Store_MovesToNextAccount_WhenFirstHasNoRoomForTheFile()
    {
        var first = await AddAccount("a", priority: 1, quota: 100 * Mb, used: 80 * Mb);
        var second = await AddAccount("b", priority: 2, quota: 100 * Mb);
        var episode = await AddEpisode(1);

        await _storer.StoreNextBatchAsync(CancellationToken.None);

        episode.StorageAccountId.ShouldBe(second.Id);
        first.UsedBytes.ShouldBe(80 * Mb);
        second.UsedBytes.ShouldBe(30 * Mb);
    }

    [Test]
    public async Task Store_FallsBackToNextAccount_WhenUploadFails()
    {
        var first = await AddAccount("a", priority: 1, quota: 100 * Mb);
        var second = await AddAccount("b", priority: 2, quota: 100 * Mb);
        var episode = await AddEpisode(1);
        _storage.Setup(x => x.UploadFileAsync(It.Is<VideoStorageTarget>(t => t.SecretAccessKey == "secret-a"), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VideoStorageException("sai khoá"));

        await _storer.StoreNextBatchAsync(CancellationToken.None);

        episode.StorageAccountId.ShouldBe(second.Id);
        episode.StorageError.ShouldBeNull();
        first.LastError.ShouldBe("sai khoá");
        first.UsedBytes.ShouldBe(0);
    }

    [Test]
    public async Task Store_RecordsError_WhenNoAccountFits_AndStopsAfterMaxAttempts()
    {
        await AddAccount("a", priority: 1, quota: 100 * Mb, used: 90 * Mb);
        var episode = await AddEpisode(1);

        await _storer.StoreNextBatchAsync(CancellationToken.None);

        episode.StreamUrl.ShouldBeNull();
        episode.StorageAttempts.ShouldBe(1);
        episode.StorageError.ShouldNotBeNull().ShouldContain("30 MB");
        _storage.Verify(x => x.UploadFileAsync(It.IsAny<VideoStorageTarget>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        (await _storer.StoreNextBatchAsync(CancellationToken.None)).ShouldBe(1);
        (await _storer.StoreNextBatchAsync(CancellationToken.None)).ShouldBe(1);
        episode.StorageAttempts.ShouldBe(3);
        // Đủ số lần lỗi: không thử nữa.
        (await _storer.StoreNextBatchAsync(CancellationToken.None)).ShouldBe(0);
    }

    [Test]
    public async Task Store_RecordsTikTokError_WithoutTouchingStorage()
    {
        var account = await AddAccount("a", priority: 1, quota: 100 * Mb);
        var episode = await AddEpisode(1);
        _tikTok.Setup(x => x.DownloadVideoAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TikTokApiException("tập bị khoá"));

        await _storer.StoreNextBatchAsync(CancellationToken.None);

        episode.StorageError.ShouldBe("tập bị khoá");
        episode.StorageAttempts.ShouldBe(1);
        episode.StorageKey.ShouldBeNull();
        account.UsedBytes.ShouldBe(0);
    }

    [Test]
    public async Task Store_GivesUpAtOnce_WhenTikTokWithholdsTheFile()
    {
        await AddAccount("a", priority: 1, quota: 100 * Mb);
        var episode = await AddEpisode(1);
        _tikTok.Setup(x => x.DownloadVideoAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TikTokVideoUnavailableException("không cấp link tải"));

        await _storer.StoreNextBatchAsync(CancellationToken.None);

        episode.StorageAttempts.ShouldBe(3);
        (await _storer.StoreNextBatchAsync(CancellationToken.None)).ShouldBe(0);
    }

    [Test]
    public async Task Batch_Stops_WithoutCountingAnAttempt_WhenTikTokBlocksTheServer()
    {
        await AddAccount("a", priority: 1, quota: 100 * Mb);
        var first = await AddEpisode(1);
        var second = await AddEpisode(2);
        _tikTok.Setup(x => x.DownloadVideoAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TikTokBlockedException("trang thử thách"));

        await Should.ThrowAsync<TikTokBlockedException>(() => _storer.StoreNextBatchAsync(CancellationToken.None));

        first.StorageAttempts.ShouldBe(0);
        first.StorageError.ShouldBe("trang thử thách");
        second.StorageError.ShouldBeNull();
        _tikTok.Verify(x => x.DownloadVideoAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Batch_SkipsEverything_WhenOwnerHasNoEnabledAccountWithSpace()
    {
        await AddAccount("đầy", priority: 1, quota: 100 * Mb, used: 100 * Mb);
        await AddAccount("tắt", priority: 2, quota: 100 * Mb, enabled: false);
        await AddAccount("người-khác", priority: 1, quota: 100 * Mb, owner: "user-2");
        await AddEpisode(1);

        (await _storer.StoreNextBatchAsync(CancellationToken.None)).ShouldBe(0);

        _tikTok.Verify(x => x.DownloadVideoAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Batch_SkipsEpisodesThatAlreadyHaveAStream()
    {
        await AddAccount("a", priority: 1, quota: 100 * Mb);
        var manual = await AddEpisode(1);
        manual.StreamUrl = "https://cdn.example.com/1.mp4";
        await _db.SaveChangesAsync();
        var pending = await AddEpisode(2);

        (await _storer.StoreNextBatchAsync(CancellationToken.None)).ShouldBe(1);

        manual.StreamUrl.ShouldBe("https://cdn.example.com/1.mp4");
        pending.StreamUrl.ShouldNotBeNull();
    }

    [Test]
    public async Task Release_DeletesFile_ReturnsSpace_AndClearsEpisode()
    {
        var account = await AddAccount("a", priority: 1, quota: 100 * Mb);
        var episode = await AddEpisode(1);
        await _storer.StoreNextBatchAsync(CancellationToken.None);
        var key = episode.StorageKey!;

        await _storer.ReleaseAsync([episode], CancellationToken.None);
        await _db.SaveChangesAsync();

        _storage.Verify(x => x.DeleteAsync(It.IsAny<VideoStorageTarget>(), key, It.IsAny<CancellationToken>()), Times.Once);
        account.UsedBytes.ShouldBe(0);
        episode.StreamUrl.ShouldBeNull();
        episode.StorageAccountId.ShouldBeNull();
        episode.StorageKey.ShouldBeNull();
    }

    [Test]
    public async Task Release_StillFreesTheEpisode_WhenDeletingTheFileFails()
    {
        var account = await AddAccount("a", priority: 1, quota: 100 * Mb);
        var episode = await AddEpisode(1);
        await _storer.StoreNextBatchAsync(CancellationToken.None);
        _storage.Setup(x => x.DeleteAsync(It.IsAny<VideoStorageTarget>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VideoStorageException("mất mạng"));

        await _storer.ReleaseAsync([episode], CancellationToken.None);

        episode.StorageAccountId.ShouldBeNull();
        account.UsedBytes.ShouldBe(0);
    }
}
