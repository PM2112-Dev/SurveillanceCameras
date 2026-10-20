using System.Net;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.Config;
using SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;

namespace SurveillanceCameras.Infrastructure.IntegrationTests.Repository.CrawlStory;

/// <summary>
/// Chạy cấu hình mẫu wikicv với HTML đã lưu từ trang thật (khảo sát 2026-10-20), không cần mạng.
/// Bản chạy với trang thật nằm ở <see cref="ConfigurableCrawlStoryRepositoryLiveTests"/>.
/// </summary>
public class WikicvCrawlOfflineTests
{
    private const string Origin = "https://wikicv.org";
    private const string RankingUrl = Origin + "/bang-xep-hang?gender=g1&so=4";

    private static readonly string PresetPath = Path.Combine(
        RepositoryRoot(), "src/Web/ClientApp/src/pages/WebSources/crawlPresets/wikicv.json");

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir is not null && !dir.EnumerateFiles("*.slnx").Any()) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Không tìm thấy thư mục gốc repo (*.slnx).");
    }

    private static CrawlContext Context(string? cookie = "sessionid=x")
    {
        var config = CrawlConfig.Parse(File.ReadAllText(PresetPath));
        // Preset giãn cách 3 giây giữa hai request; test offline không cần chờ.
        Assert.That(config.RequestDelayMs, Is.GreaterThanOrEqualTo(2000), "Preset wikicv phải giãn cách request.");
        config.RequestDelayMs = 0;
        return new CrawlContext(RankingUrl, cookie, config);
    }

    /// <summary>Một trang bảng xếp hạng: 20 truyện kèm khung phân trang kiểu wikicv (cửa sổ 10 số, nút sang phải và trang cuối).</summary>
    private static string RankingPage(int page, int lastPage)
    {
        var items = string.Concat(Enumerable.Range(0, 20).Select(i =>
            $"""
             <div class="book-item"><div class="cover-col"><a class="cover-wrapper" href="/truyen/t{page}-{i}-abc"><img src="/photo/p{page}{i}"></a></div>
             <div class="info-col"><a class="tooltipped" href="/truyen/t{page}-{i}-abc"><h5 class="book-title">Truyện {page}-{i}</h5></a>
             <p class="book-author"><a href="/tac-gia/x">Tác giả {i}</a></p>
             <p class="book-publisher"><a href="/tim-kiem?status=s1">Còn tiếp</a></p></div></div>
             """));

        string Link(int target, string inner) =>
            $"<li class=\"waves-effect\"><a href=\" /bang-xep-hang?gender=5794f03d&amp;vo=1&amp;tr=4&amp;so=4&amp;start={(target - 1) * 20} \">{inner}</a></li>";

        var pagination = new System.Text.StringBuilder();
        if (page > 1) pagination.Append(Link(1, "«")).Append(Link(page - 1, "‹"));
        for (var p = Math.Max(1, page - 4); p <= Math.Min(lastPage, Math.Max(10, page + 5)); p++)
            pagination.Append(p == page ? $"<li class=\" active \"><a href=\" #! \">{p}</a></li>" : Link(p, p.ToString()));
        if (page < lastPage) pagination.Append(Link(page + 1, "›")).Append(Link(lastPage, "»"));

        return $"<html><body><main><div>{items}</div><ul class=\"pagination\">{pagination}</ul></main></body></html>";
    }

    private const string LoginPage =
        "<html><body><main><div class=\"container\"><div style=\"text-align:center\">Đăng nhập để xem nội dung</div></div></main></body></html>";

    private static ConfigurableCrawlStoryRepository Repository(Func<Uri, string> respond)
        => new(new FakeFactory(respond));

    [Test]
    public async Task RankingList_ReadsTotalPagesFromPaginationLinks()
    {
        var repository = Repository(uri => RankingPage(int.Parse(Query(uri, "start") ?? "0") / 20 + 1, lastPage: 50));

        var first = await repository.FetchStoryListAsync(Context(), 1);
        Assert.That(first.Items, Has.Count.EqualTo(20));
        Assert.That(first.TotalPages, Is.EqualTo(50));
        Assert.That(first.HasNextPage, Is.True);
        Assert.That(first.Items[0].Link, Is.EqualTo(Origin + "/truyen/t1-0-abc"));
        Assert.That(first.Items[0].Status, Is.EqualTo("Còn tiếp"));

        // Trang cuối: khung phân trang không liệt kê chính nó nhưng tổng vẫn là 50 và hết trang.
        var last = await repository.FetchStoryListAsync(Context(), 50);
        Assert.That(last.TotalPages, Is.EqualTo(50));
        Assert.That(last.HasNextPage, Is.False);
        Assert.That(last.Items, Has.Count.EqualTo(20));
    }

    [Test]
    public async Task RankingList_PageBeyondTheEnd_ReturnsNothingInsteadOfRepeatingTheLastPage()
    {
        // Nguồn trả lại trang cuối khi xin vượt quá nó.
        var repository = Repository(uri => RankingPage(Math.Min(int.Parse(Query(uri, "start") ?? "0") / 20 + 1, 50), lastPage: 50));

        var beyond = await repository.FetchStoryListAsync(Context(), 52);

        Assert.That(beyond.Items, Is.Empty);
        Assert.That(beyond.HasNextPage, Is.False);
    }

    [Test]
    public void LoginPage_WithoutCookie_FailsWithAClearMessage()
    {
        var repository = Repository(_ => LoginPage);

        var ex = Assert.ThrowsAsync<CrawlException>(() => repository.FetchStoryListAsync(Context(cookie: null), 1));

        Assert.That(ex!.Message, Does.Contain("yêu cầu đăng nhập").And.Contain("Cần cookie"));
    }

    [Test]
    public void LoginPage_WithCookie_SaysTheCookieExpired()
    {
        var repository = Repository(_ => LoginPage);

        var ex = Assert.ThrowsAsync<CrawlException>(() => repository.FetchStoryListAsync(Context(), 1));

        Assert.That(ex!.Message, Does.Contain("hết hạn"));
    }

    [Test]
    public async Task Chapter_DropsRepeatedTitleLineAndAds()
    {
        const string chapter = """
            <html><body><div id="bookContent"><p class="book-title">Truyện A</p><p class="book-title">Chương 1 tên chương</p>
            <p class="book-title">Tác giả: B</p>
            <div id="bookContentBody"><p>Chương 1 tên chương</p><p>Đoạn một.</p>
            <div class="gliaplayer-container">QUẢNG CÁO</div><div class="tpm-unit">QUẢNG CÁO</div><p>Đoạn hai.</p></div></div></body></html>
            """;
        var repository = Repository(_ => chapter);

        var result = await repository.FetchChapterAsync(Context(), Origin + "/truyen/a/chuong-1");

        Assert.That(result.Title, Is.EqualTo("Chương 1 tên chương"));
        Assert.That(result.Paragraphs, Is.EqualTo(new[] { "Đoạn một.", "Đoạn hai." }));
    }

    [Test]
    public async Task Chapter_KeepsFirstLineWhenItIsNotTheTitle()
    {
        const string chapter = """
            <html><body><div id="bookContent"><p class="book-title">Truyện A</p><p class="book-title">Chương 2 khác</p>
            <div id="bookContentBody"><p>Ngày mùa hè chạng vạng.</p><p>Đoạn hai.</p></div></div></body></html>
            """;
        var repository = Repository(_ => chapter);

        var result = await repository.FetchChapterAsync(Context(), Origin + "/truyen/a/chuong-2");

        Assert.That(result.Paragraphs, Is.EqualTo(new[] { "Ngày mùa hè chạng vạng.", "Đoạn hai." }));
    }

    private static string? Query(Uri uri, string name)
        => uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .FirstOrDefault(parts => parts[0] == name)?[1];

    private sealed class FakeFactory(Func<Uri, string> respond) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new FakeHandler(respond));
    }

    private sealed class FakeHandler(Func<Uri, string> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(respond(request.RequestUri!), System.Text.Encoding.UTF8, "text/html")
            });
    }
}
