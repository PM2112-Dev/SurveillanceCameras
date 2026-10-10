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
}
