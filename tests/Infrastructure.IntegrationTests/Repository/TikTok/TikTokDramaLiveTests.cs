using Microsoft.Extensions.Options;
using SurveillanceCameras.Infrastructure.Repository.TikTok;

namespace SurveillanceCameras.Infrastructure.IntegrationTests.Repository.TikTok;

/// <summary>Gọi TikTok thật (không cần cookie). Chạy: dotnet test --filter Category=LiveTikTok</summary>
[Category("LiveTikTok")]
[Explicit("Gọi TikTok thật")]
public class TikTokDramaLiveTests
{
    // Kênh stardusttv_official (khảo sát 2026-09-30): drama 30 tập.
    private const string DramaId = "7677108486182392852";

    [Test]
    public async Task FetchDramaEpisodes_ReturnsNumberedEpisodes_AcrossPages()
    {
        var options = new TikTokApiOptions();
        var repository = new TikTokRepository(Options.Create(options),
            new HttpClient { BaseAddress = new Uri(options.BaseUrl) });

        var first = await repository.FetchDramaEpisodes(null, DramaId, 10, "0");
        var second = await repository.FetchDramaEpisodes(null, DramaId, 10, first.Cursor!);

        Assert.That(first.ItemList, Has.Count.EqualTo(10));
        Assert.That(first.HasMore, Is.True);
        Assert.That(first.ItemList.Select(x => x.EpisodeNumber), Is.EqualTo(Enumerable.Range(1, 10).Cast<int?>()));
        Assert.That(second.ItemList[0].EpisodeNumber, Is.EqualTo(11));
        Assert.That(first.ItemList[0].Video!.Duration, Is.GreaterThan(0));
        Assert.That(first.ItemList[0].DramaInfo!.DramaName, Is.Not.Empty);
        Assert.That(first.ItemList[0].Author!.UniqueId, Is.EqualTo("stardusttv_official"));
    }

    [Test]
    public async Task DownloadVideo_SavesTheFile_ForAPublicVideo()
    {
        var options = new TikTokApiOptions();
        var repository = new TikTokRepository(Options.Create(options),
            new HttpClient { BaseAddress = new Uri(options.BaseUrl) });
        var path = Path.Combine(Path.GetTempPath(), $"tiktok-live-{Guid.NewGuid():N}.mp4");

        try
        {
            var size = await repository.DownloadVideoAsync(null,
                "https://www.tiktok.com/@scout2015/video/6718335390845095173", path, 50 * 1024 * 1024);

            Assert.That(size, Is.GreaterThan(100_000));
            Assert.That(new FileInfo(path).Length, Is.EqualTo(size));
        }
        finally
        {
            File.Delete(path);
        }
    }

    // TikTok không cấp link file của tập drama cho người chưa đăng nhập (kể cả tập xem thử).
    [Test]
    public void DownloadVideo_Fails_ForADramaEpisode_WithoutAnAccountCookie()
    {
        var options = new TikTokApiOptions();
        var repository = new TikTokRepository(Options.Create(options),
            new HttpClient { BaseAddress = new Uri(options.BaseUrl) });
        var path = Path.Combine(Path.GetTempPath(), $"tiktok-live-{Guid.NewGuid():N}.mp4");

        var ex = Assert.CatchAsync<SurveillanceCameras.Application.Common.Exceptions.TikTokApiException>(() =>
            repository.DownloadVideoAsync(null, "https://www.tiktok.com/@stardusttv_official/video/7677109886609673485", path, 50 * 1024 * 1024));

        Assert.That(ex!.Message, Does.Contain("không cấp link tải"));
        Assert.That(File.Exists(path), Is.False);
    }
}
