using System.Diagnostics;
using System.Net;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.Config;
using SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;
using Microsoft.Extensions.Options;

namespace SurveillanceCameras.Infrastructure.IntegrationTests.Repository.CrawlStory;

public class CrawlRequestThrottleTests
{
    private const string Host = "throttle-test.example";

    [SetUp, TearDown]
    public void ResetHost() => CrawlRequestThrottle.Reset(Host);

    [Test]
    public async Task ConcurrentCallers_AreSpacedByTheInterval()
    {
        var interval = TimeSpan.FromMilliseconds(120);
        var watch = Stopwatch.StartNew();

        // 4 luồng cùng xin một lúc: ô giờ đầu ngay, các ô sau cách nhau ít nhất `interval`.
        var finished = await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ =>
        {
            await CrawlRequestThrottle.WaitAsync(Host, interval, CancellationToken.None);
            return watch.Elapsed;
        }));

        var ordered = finished.Order().ToList();
        for (var i = 1; i < ordered.Count; i++)
            Assert.That(ordered[i] - ordered[i - 1], Is.GreaterThanOrEqualTo(interval - TimeSpan.FromMilliseconds(25)),
                $"Ô giờ {i} quá sát ô trước");
        Assert.That(ordered[^1], Is.GreaterThanOrEqualTo(interval * 3 - TimeSpan.FromMilliseconds(60)));
    }

    [Test]
    public async Task ZeroInterval_DoesNotWait()
    {
        var watch = Stopwatch.StartNew();
        for (var i = 0; i < 20; i++) await CrawlRequestThrottle.WaitAsync(Host, TimeSpan.Zero, CancellationToken.None);
        Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromMilliseconds(200)));
    }

    [Test]
    public void BlockedHost_RefusesUntilTheCooldownEnds_AndOtherHostsAreUnaffected()
    {
        CrawlRequestThrottle.Block(Host, TimeSpan.FromMinutes(5));

        var ex = Assert.ThrowsAsync<TimeoutException>(() => CrawlRequestThrottle.WaitAsync(Host, TimeSpan.Zero, CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain(Host).And.Contain("tạm nghỉ"));
        Assert.DoesNotThrowAsync(() => CrawlRequestThrottle.WaitAsync("other-host.example", TimeSpan.Zero, CancellationToken.None));
    }

    [Test]
    public void Options_UseTheLargerOfHostSettingAndSourceSetting()
    {
        var options = new CrawlStoryOptions
        {
            DefaultIntervalMs = 200,
            HostIntervalsMs = new(StringComparer.OrdinalIgnoreCase) { ["wikicv.org"] = 3000 }
        };

        Assert.That(options.IntervalFor("WIKICV.org", null), Is.EqualTo(TimeSpan.FromSeconds(3)));
        Assert.That(options.IntervalFor("wikicv.org", 5000), Is.EqualTo(TimeSpan.FromSeconds(5)));
        Assert.That(options.IntervalFor("wikicv.org", 1000), Is.EqualTo(TimeSpan.FromSeconds(3)));
        Assert.That(options.IntervalFor("khac.test", null), Is.EqualTo(TimeSpan.FromMilliseconds(200)));
        Assert.That(options.IntervalFor("khac.test", 800), Is.EqualTo(TimeSpan.FromMilliseconds(800)));
    }

    [Test]
    public async Task Repository_SpacesRequests_AndStopsCallingAfterA429()
    {
        var sent = new List<DateTimeOffset>();
        var respond429 = false;
        var repository = new ConfigurableCrawlStoryRepository(
            new FakeFactory(_ =>
            {
                sent.Add(DateTimeOffset.UtcNow);
                return respond429 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK;
            }),
            Options.Create(new CrawlStoryOptions()));
        var config = CrawlConfig.Parse("""
            { "requestDelayMs": 150, "storyList": { "item": ".x", "title": ".t", "link": "a@href" } }
            """);
        var context = new CrawlContext($"https://{Host}/danh-sach", null, config);

        await repository.FetchStoryListAsync(context, 1);
        await repository.FetchStoryListAsync(context, 1);
        Assert.That(sent[1] - sent[0], Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(130)));

        respond429 = true;
        var ex = Assert.ThrowsAsync<CrawlException>(() => repository.FetchStoryListAsync(context, 1));
        Assert.That(ex!.Message, Does.Contain("429").And.Contain("tạm nghỉ"));
        var afterBlock = sent.Count;

        // Đang nghỉ: không gửi thêm request nào nữa.
        var again = Assert.ThrowsAsync<CrawlException>(() => repository.FetchStoryListAsync(context, 1));
        Assert.That(again!.Message, Does.Contain("tạm nghỉ"));
        Assert.That(sent, Has.Count.EqualTo(afterBlock));
    }

    private sealed class FakeFactory(Func<Uri, HttpStatusCode> status) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new FakeHandler(status));
    }

    private sealed class FakeHandler(Func<Uri, HttpStatusCode> status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status(request.RequestUri!))
            {
                Content = new StringContent("<html><body></body></html>", System.Text.Encoding.UTF8, "text/html")
            });
    }
}
