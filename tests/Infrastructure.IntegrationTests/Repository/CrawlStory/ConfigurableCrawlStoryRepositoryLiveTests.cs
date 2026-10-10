using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.Config;
using SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;

namespace SurveillanceCameras.Infrastructure.IntegrationTests.Repository.CrawlStory;

/// <summary>
/// Chạy cấu hình mẫu (crawlPresets/*.json của FE) với trang thật để kiểm tra selector còn đúng.
/// Gọi mạng thật nên chỉ chạy khi chỉ định: <c>dotnet test --filter Category=LiveCrawl</c>.
/// </summary>
[TestFixture, Explicit, Category("LiveCrawl")]
public class ConfigurableCrawlStoryRepositoryLiveTests
{
    private static readonly string PresetDir = Path.Combine(
        RepositoryRoot(), "src/Web/ClientApp/src/pages/WebSources/crawlPresets");

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir is not null && !dir.EnumerateFiles("*.slnx").Any()) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Không tìm thấy thư mục gốc repo (*.slnx).");
    }

    private static CrawlContext Context(string preset, string baseUrl)
        => new(baseUrl, null, CrawlConfig.Parse(File.ReadAllText(Path.Combine(PresetDir, preset + ".json"))));

    private static ConfigurableCrawlStoryRepository CreateRepository() => new(new BrowserLikeClientFactory());

    [TestCase("wikidichzz", "https://wikidichzz.com/danh-sach/truyen-full")]
    [TestCase("wikicv", "https://wikicv.org/truyen-nam")]
    public async Task FullFlow_ReturnsData(string preset, string baseUrl)
    {
        var repository = CreateRepository();
        var context = Context(preset, baseUrl);

        var list = await repository.FetchStoryListAsync(context, 1);
        Log($"LIST p1: {list.Items.Count} items, total={list.TotalPages}, next={list.HasNextPage}, src={list.SourceUrl}");
        foreach (var s in list.Items.Take(3)) Log($"  - {s.Title} | {s.Author} | {s.Status} | {s.ChapterCount} | {s.Link} | {s.CoverUrl}");
        Assert.That(list.Items, Is.Not.Empty);
        Assert.That(list.Items.All(s => !string.IsNullOrWhiteSpace(s.Title) && !string.IsNullOrWhiteSpace(s.Link)));

        if (list.HasNextPage)
        {
            var page2 = await repository.FetchStoryListAsync(context, 2);
            Log($"LIST p2: {page2.Items.Count} items, first={page2.Items.FirstOrDefault()?.Title}, src={page2.SourceUrl}");
            Assert.That(page2.Items, Is.Not.Empty);
            // Hai trang liên tiếp không được trùng truyện (bắt lỗi CDN trả bản cache cũ của một trang).
            var overlap = page2.Items.Select(s => s.Link).Intersect(list.Items.Select(s => s.Link)).Count();
            Assert.That(overlap, Is.Zero, "Trang 1 và trang 2 trùng truyện — có thể trang nguồn đang trả bản cache cũ.");
        }

        var storyUrl = list.Items[0].Link!;
        var story = await repository.FetchStoryAsync(context, storyUrl);
        Log($"STORY: {story.Title} | {story.Author} | {story.Status} | chapters={story.ChapterCount} | cover={story.CoverUrl}");
        Log($"  genres: {string.Join(", ", story.Genres)}");
        Log($"  desc: {story.Description?[..Math.Min(200, story.Description.Length)]}");
        Assert.That(story.Title, Is.Not.Empty);

        var chapters = await repository.FetchChapterListAsync(context, storyUrl, 1);
        Log($"CHAPTERS p1: {chapters.Items.Count}, total={chapters.TotalPages}, next={chapters.HasNextPage}, src={chapters.SourceUrl}");
        foreach (var c in chapters.Items.Take(3)) Log($"  - {c.Title} | {c.Link}");
        Assert.That(chapters.Items, Is.Not.Empty);

        if (chapters.HasNextPage)
        {
            var chaptersPage2 = await repository.FetchChapterListAsync(context, storyUrl, 2);
            Log($"CHAPTERS p2: {chaptersPage2.Items.Count}, first={chaptersPage2.Items.FirstOrDefault()?.Title}");
            Assert.That(chaptersPage2.Items, Is.Not.Empty);
        }

        var chapter = await repository.FetchChapterAsync(context, chapters.Items[0].Link!);
        Log($"CHAPTER: {chapter.Title} — {chapter.Paragraphs.Count} đoạn");
        foreach (var p in chapter.Paragraphs.Take(2)) Log($"  > {p[..Math.Min(150, p.Length)]}");
        Assert.That(chapter.Paragraphs, Is.Not.Empty);
    }

    /// <summary>Giống lúc đồng bộ: tải chi tiết cả một trang truyện song song (5 luồng) trên cùng một repository.</summary>
    [TestCase("wikidichzz", "https://wikidichzz.com/danh-sach/truyen-full")]
    public async Task StoryDetails_InParallel_AllHaveDescription(string preset, string baseUrl)
    {
        var repository = CreateRepository();
        var context = Context(preset, baseUrl);
        var list = await repository.FetchStoryListAsync(context, 1);

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var details = new System.Collections.Concurrent.ConcurrentBag<Application.Repository.CrawlStory.DTOs.StoryDataDto>();
        await Parallel.ForEachAsync(list.Items, new ParallelOptions { MaxDegreeOfParallelism = 5 },
            async (story, ct) => details.Add(await repository.FetchStoryAsync(context, story.Link!, ct)));
        watch.Stop();

        Log($"{details.Count} truyện trong {watch.Elapsed.TotalSeconds:F1}s; có giới thiệu: {details.Count(d => !string.IsNullOrWhiteSpace(d.Description))}");
        Assert.That(details, Has.Count.EqualTo(list.Items.Count));
        Assert.That(details.All(d => !string.IsNullOrWhiteSpace(d.Description)));
    }

    private static void Log(string message) => TestContext.Out.WriteLine(message);

    private sealed class BrowserLikeClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            var client = new HttpClient(new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                AutomaticDecompression = System.Net.DecompressionMethods.All
            });
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0 Safari/537.36");
            return client;
        }
    }
}
