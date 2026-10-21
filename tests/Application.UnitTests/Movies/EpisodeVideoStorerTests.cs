using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Models.TikTok;
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
    private const long Mb = StorageAccount.BytesPerMb;

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
            Options.Create(new EpisodeVideoStoreOptions { DelayBetweenVideosSeconds = 0, MaxAttempts = 3, MinFreeBytes = 1, BatchSize = 10 }),
            new DramaFileCache(TimeProvider.System), NullLogger<EpisodeVideoStorer>.Instance);

        _movie = new Work { Title = "Phim", Type = WorkType.Movie, CreatedBy = Owner, Episodes = [] };
        _db.Works.Add(_movie);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private DateTimeOffset _now = new(2026, 10, 21, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Một lượt như khi chạy thật: lượt quét xếp hàng các tập, rồi consumer xử lý lần lượt từng tập (mỗi lượt cách 2 giờ).</summary>
    private async Task<int> RunBatchAsync(CancellationToken cancellationToken)
    {
        _now = _now.AddHours(2);
        var queued = await _storer.QueuePendingAsync(_now, cancellationToken);
        var ids = _db.Episodes.Where(x => x.QueuedAt == _now).OrderBy(x => x.StorageAttempts).ThenBy(x => x.WorkId).ThenBy(x => x.EpisodeNumber)
            .Select(x => x.Id).ToList();
        foreach (var id in ids) await _storer.StoreQueuedAsync(id, cancellationToken);
        return queued;
    }

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

        (await RunBatchAsync(CancellationToken.None)).ShouldBe(1);

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

        await RunBatchAsync(CancellationToken.None);

        _tikTok.Verify(x => x.DownloadVideoAsync("sessionid=moi", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private const string DramaId = "7689353658332910600";

    /// <summary>Biến phim thành drama TikTok và thêm tài khoản TikTok đang đăng nhập (nếu có cookie).</summary>
    private async Task MakeDrama(string? cookie)
    {
        _movie.ExternalId = DramaId;
        if (cookie is not null)
        {
            _db.Accounts.Add(new Account
            {
                Title = "TikTok", Cookie = cookie, CreatedBy = Owner,
                AccountType = new AccountType { Title = "TikTok", BaseUrl = "https://www.tiktok.com/login", CreatedBy = Owner }
            });
        }

        await _db.SaveChangesAsync();
    }

    private static DramaEpisodeDto Item(string id, params string[] h264Urls) => new()
    {
        Id = id,
        Video = new DramaEpisodeVideoDto
        {
            PlayAddr = h264Urls.Length > 0 ? "https://v16.tiktok.com/default.mp4" : null,
            BitrateInfo = h264Urls.Length > 0
                ? [new DramaVideoBitrateDto { CodecType = "h264", Bitrate = 900_000, PlayAddr = new DramaVideoPlayAddrDto { UrlList = [.. h264Urls] } }]
                : []
        }
    };

    private void SetupEpisodeList(string? cookie, params DramaEpisodeDto[] items)
        => _tikTok.Setup(x => x.FetchDramaEpisodes(cookie, DramaId, It.IsAny<int>(), "0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DramaEpisodeListDto { ItemList = [.. items], HasMore = false });

    [Test]
    public async Task Store_DramaEpisode_GetsFileLinksFromTheEpisodeApi_WithTheAccountCookie()
    {
        await AddAccount("a", priority: 1, quota: 100 * Mb);
        await MakeDrama("sessionid=abc");
        var first = await AddEpisode(1);
        var second = await AddEpisode(2);
        SetupEpisodeList("sessionid=abc", Item(first.ExternalId!, "https://v16.tiktok.com/1.mp4"), Item(second.ExternalId!, "https://v16.tiktok.com/2.mp4"));
        _tikTok.Setup(x => x.DownloadVideoFileAsync(It.IsAny<string?>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _videoSize);

        (await RunBatchAsync(CancellationToken.None)).ShouldBe(2);

        first.StreamUrl.ShouldNotBeNull();
        second.StreamUrl.ShouldNotBeNull();
        _tikTok.Verify(x => x.DownloadVideoFileAsync("sessionid=abc",
            It.Is<IReadOnlyList<string>>(urls => urls[0] == "https://v16.tiktok.com/1.mp4"), It.IsAny<string>(), It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Once);
        // Danh sách tập chỉ đọc một lần cho cả lô; không mở trang video.
        _tikTok.Verify(x => x.FetchDramaEpisodes(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _tikTok.Verify(x => x.DownloadVideoAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Store_DramaEpisode_StopsWithoutCountingAnAttempt_WhenNoTikTokAccountIsLoggedIn()
    {
        await AddAccount("a", priority: 1, quota: 100 * Mb);
        await MakeDrama(cookie: null);
        var episode = await AddEpisode(1);

        await Should.ThrowAsync<TikTokBlockedException>(() => RunBatchAsync(CancellationToken.None));

        episode.StorageAttempts.ShouldBe(0);
        episode.StorageError.ShouldNotBeNull().ShouldContain("đăng nhập");
        _tikTok.Verify(x => x.FetchDramaEpisodes(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Store_DramaEpisode_StopsWithoutCountingAnAttempt_WhenTheCookieHasExpired()
    {
        await AddAccount("a", priority: 1, quota: 100 * Mb);
        await MakeDrama("sessionid=het-han");
        var episode = await AddEpisode(1);
        // Cookie hết hạn: TikTok vẫn trả danh sách tập nhưng không tập nào có link file.
        SetupEpisodeList("sessionid=het-han", Item(episode.ExternalId!), Item("khac"));

        await Should.ThrowAsync<TikTokBlockedException>(() => RunBatchAsync(CancellationToken.None));

        episode.StorageAttempts.ShouldBe(0);
        episode.StorageError.ShouldNotBeNull().ShouldContain("hết hạn");
    }

    [Test]
    public async Task Store_DramaEpisode_GivesUp_WhenOnlyThatEpisodeHasNoFile()
    {
        await AddAccount("a", priority: 1, quota: 100 * Mb);
        await MakeDrama("sessionid=abc");
        var episode = await AddEpisode(1);
        SetupEpisodeList("sessionid=abc", Item(episode.ExternalId!), Item("khac", "https://v16.tiktok.com/khac.mp4"));

        await RunBatchAsync(CancellationToken.None);

        episode.StorageAttempts.ShouldBe(3);
        episode.StreamUrl.ShouldBeNull();
    }

    [Test]
    public async Task Store_MovesToNextAccount_WhenFirstHasNoRoomForTheFile()
    {
        var first = await AddAccount("a", priority: 1, quota: 100 * Mb, used: 80 * Mb);
        var second = await AddAccount("b", priority: 2, quota: 100 * Mb);
        var episode = await AddEpisode(1);

        await RunBatchAsync(CancellationToken.None);

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

        await RunBatchAsync(CancellationToken.None);

        episode.StorageAccountId.ShouldBe(second.Id);
        episode.StorageError.ShouldBeNull();
        first.LastError.ShouldBe("sai khoá");
        first.UsedBytes.ShouldBe(0);
    }

    [Test]
    public async Task Queue_SkipsEpisodes_WhenTheStoreCannotHoldAnotherVideo()
    {
        // Còn 10 MB, một video trung bình ước tính 30 MB: không xếp hàng, cũng không tải từ TikTok.
        await AddAccount("a", priority: 1, quota: 100 * Mb, used: 90 * Mb);
        var episode = await AddEpisode(1);

        (await RunBatchAsync(CancellationToken.None)).ShouldBe(0);

        episode.QueuedAt.ShouldBeNull();
        _db.OutboxEventTypes().ShouldBeEmpty();
        _tikTok.Verify(x => x.DownloadVideoAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Queue_LimitsTheBatchToWhatTheStoreCanHold_CountingEpisodesAlreadyWaiting()
    {
        await AddAccount("a", priority: 1, quota: 100 * Mb);   // chứa được 3 video 30 MB
        for (var i = 1; i <= 5; i++) await AddEpisode(i);

        _storer.QueuePendingAsync(_now, CancellationToken.None).Result.ShouldBe(3);
        _db.OutboxEventTypes().Count.ShouldBe(3);

        // Hàng đợi chưa xử lý xong: không xếp thêm tập nào nữa dù còn tập chờ.
        (await _storer.QueuePendingAsync(_now.AddMinutes(5), CancellationToken.None)).ShouldBe(0);
    }

    [Test]
    public async Task Queue_UsesTheAverageSizeOfVideosAlreadyStored()
    {
        // Video đã lưu trung bình 10 MB, kho còn 40 MB → chứa được 4 video.
        await AddAccount("a", priority: 1, quota: 100 * Mb, used: 60 * Mb);
        var stored = await AddEpisode(1);
        stored.StreamUrl = "https://cdn.example.com/1.mp4";
        stored.StorageSizeBytes = 10 * Mb;
        await _db.SaveChangesAsync();
        for (var i = 2; i <= 8; i++) await AddEpisode(i);

        (await _storer.QueuePendingAsync(_now, CancellationToken.None)).ShouldBe(4);
    }

    [Test]
    public async Task Store_WhenTheStoreFillsUpAfterQueueing_DoesNotCountAnAttempt_AndIsQueuedAgainLater()
    {
        var account = await AddAccount("a", priority: 1, quota: 100 * Mb);
        var episode = await AddEpisode(1);
        await _storer.QueuePendingAsync(_now, CancellationToken.None);
        account.UsedBytes = 90 * Mb;   // trong lúc chờ, kho bị chiếm chỗ
        await _db.SaveChangesAsync();

        await _storer.StoreQueuedAsync(episode.Id, CancellationToken.None);

        episode.StreamUrl.ShouldBeNull();
        episode.StorageAttempts.ShouldBe(0);
        episode.QueuedAt.ShouldBeNull();
        episode.StorageError.ShouldNotBeNull().ShouldContain("30 MB");
        _storage.Verify(x => x.UploadFileAsync(It.IsAny<VideoStorageTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        // Có thêm chỗ thì lượt quét sau xếp hàng lại và lưu được.
        account.UsedBytes = 0;
        await _db.SaveChangesAsync();
        (await RunBatchAsync(CancellationToken.None)).ShouldBe(1);
        episode.StreamUrl.ShouldNotBeNull();
    }

    [Test]
    public async Task StoreQueued_SkipsWithoutDownloading_WhenTheStoreHasNoRoomAtAll()
    {
        var account = await AddAccount("a", priority: 1, quota: 100 * Mb);
        var options = new EpisodeVideoStoreOptions { MinFreeBytes = 50 * Mb, DelayBetweenVideosSeconds = 0 };
        var storer = new EpisodeVideoStorer(_db, _tikTok.Object, _storage.Object, _protector, Options.Create(options),
            new DramaFileCache(TimeProvider.System), NullLogger<EpisodeVideoStorer>.Instance);
        var episode = await AddEpisode(1);
        account.UsedBytes = 80 * Mb;
        await _db.SaveChangesAsync();

        await storer.StoreQueuedAsync(episode.Id, CancellationToken.None);

        episode.StorageError.ShouldNotBeNull().ShouldContain("không còn đủ chỗ");
        episode.StorageAttempts.ShouldBe(0);
        _tikTok.Verify(x => x.DownloadVideoAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Queue_ReopensEpisodesThatRanOutOfAttempts_OnceAfterTheRetryInterval()
    {
        await AddAccount("a", priority: 1, quota: 100 * Mb);
        var episode = await AddEpisode(1);
        episode.StorageAttempts = 3;
        episode.StorageError = "lỗi cũ";
        await _db.SaveChangesAsync();

        // Chưa từng xếp hàng: mở lại ngay một lần thử.
        (await _storer.QueuePendingAsync(_now, CancellationToken.None)).ShouldBe(1);
        episode.StorageAttempts.ShouldBe(2);

        // Lại hết lượt thử: chưa tới 12 giờ thì chưa mở lại.
        episode.StorageAttempts = 3;
        await _db.SaveChangesAsync();
        (await _storer.QueuePendingAsync(_now.AddHours(3), CancellationToken.None)).ShouldBe(0);
        (await _storer.QueuePendingAsync(_now.AddHours(13), CancellationToken.None)).ShouldBe(1);
    }

    [Test]
    public async Task Queue_EmitsOneKafkaEventPerEpisode()
    {
        await AddAccount("a", priority: 1, quota: 100 * Mb);
        await AddEpisode(1);
        await AddEpisode(2);

        await _storer.QueuePendingAsync(_now, CancellationToken.None);

        _db.OutboxEventTypes().ShouldBe(["EpisodeVideoStoreRequestedIntegrationEvent", "EpisodeVideoStoreRequestedIntegrationEvent"]);
    }

    [Test]
    public async Task Store_RecordsTikTokError_WithoutTouchingStorage()
    {
        var account = await AddAccount("a", priority: 1, quota: 100 * Mb);
        var episode = await AddEpisode(1);
        _tikTok.Setup(x => x.DownloadVideoAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TikTokApiException("tập bị khoá"));

        await RunBatchAsync(CancellationToken.None);

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

        await RunBatchAsync(CancellationToken.None);

        episode.StorageAttempts.ShouldBe(3);
        (await RunBatchAsync(CancellationToken.None)).ShouldBe(0);
    }

    [Test]
    public async Task Batch_Stops_WithoutCountingAnAttempt_WhenTikTokBlocksTheServer()
    {
        await AddAccount("a", priority: 1, quota: 100 * Mb);
        var first = await AddEpisode(1);
        var second = await AddEpisode(2);
        _tikTok.Setup(x => x.DownloadVideoAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TikTokBlockedException("trang thử thách"));

        await Should.ThrowAsync<TikTokBlockedException>(() => RunBatchAsync(CancellationToken.None));

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

        (await RunBatchAsync(CancellationToken.None)).ShouldBe(0);

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

        (await RunBatchAsync(CancellationToken.None)).ShouldBe(1);

        manual.StreamUrl.ShouldBe("https://cdn.example.com/1.mp4");
        pending.StreamUrl.ShouldNotBeNull();
    }

    [Test]
    public async Task Release_DeletesFile_ReturnsSpace_AndClearsEpisode()
    {
        var account = await AddAccount("a", priority: 1, quota: 100 * Mb);
        var episode = await AddEpisode(1);
        await RunBatchAsync(CancellationToken.None);
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
        await RunBatchAsync(CancellationToken.None);
        _storage.Setup(x => x.DeleteAsync(It.IsAny<VideoStorageTarget>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VideoStorageException("mất mạng"));

        await _storer.ReleaseAsync([episode], CancellationToken.None);

        episode.StorageAccountId.ShouldBeNull();
        account.UsedBytes.ShouldBe(0);
    }
}
