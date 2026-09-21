using Microsoft.Extensions.DependencyInjection;
using SurveillanceCameras.Application.Service.TikTokApi;
using SurveillanceCameras.Application.TikTokApi;
using SurveillanceCameras.Infrastructure.Service.TikTokApi;

namespace SurveillanceCameras.Infrastructure.IntegrationTests.Service.TikTokApi;

/// <summary>
/// Test tích hợp thật: đọc cookie của Account trong database thật rồi gọi TikTok thật.
/// Cần Postgres đang chạy (connection string trong src/Web/appsettings.json) và mạng internet.
/// <para>
/// Tuỳ chọn (biến môi trường): <c>TIKTOK_TEST_ACCOUNT_ID</c> (mặc định: account đầu tiên có cookie),
/// <c>TIKTOK_TEST_SEC_UID</c> (mặc định: kênh @stardusttv_vietnam).
/// </para>
/// </summary>
[TestFixture]
[Category("Integration")]
public class TikTokApiServiceIntegrationTests
{
    // secUid công khai của kênh @stardusttv_vietnam (có nhiều drama để test phân trang).
    private const string DefaultSecUid =
        "MS4wLjABAAAA7s0_xRU3BcsfFJV3Suu_z3E9lDorIFsiJ11lOCJfGHX6QGTMpxnRQ1bftpjlgFfe";

    private ServiceProvider _services = null!;
    private ITikTokApiService _tikTokApi = null!;
    private string _secUid = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        _services = new ServiceCollection()
            .AddTikTokApi(TestEnvironment.Configuration)
            .BuildServiceProvider();

        _tikTokApi = _services.GetRequiredService<ITikTokApiService>();
        _secUid = Environment.GetEnvironmentVariable("TIKTOK_TEST_SEC_UID") is { Length: > 0 } fromEnv
            ? fromEnv
            : DefaultSecUid;
    }

    [OneTimeTearDown]
    public async Task TearDown() => await _services.DisposeAsync();

    [Test]
    public async Task Cookie_LoadedFromDatabase_BuildsHeaderWithSessionId()
    {
        var account = await TestAccounts.LoadWithCookieAsync();

        var header = TikTokCookieHeader.Build(account.Cookie);

        // Không in giá trị cookie ra log, chỉ kiểm tra có cookie đăng nhập.
        Assert.That(header, Does.Contain("sessionid="), "Cookie trong DB không có sessionid (chưa đăng nhập TikTok?)");
    }

    [Test]
    public async Task GetDramaList_WithCookieFromDatabase_ReturnsRealData()
    {
        var account = await TestAccounts.LoadWithCookieAsync();

        var result = await _tikTokApi.GetDramaListAsync(account.Cookie, _secUid, count: 20, cursor: "0");

        TestContext.Out.WriteLine(
            $"Account #{account.Id}: {result.DramaList.Count} drama, cursor={result.Cursor}, hasMore={result.HasMore}");
        foreach (var drama in result.DramaList.Take(5))
        {
            TestContext.Out.WriteLine(
                $"  [{drama.DramaId}] {drama.DramaName} — {drama.NumVideos} tập, {drama.NumWatched} lượt xem");
        }

        Assert.That(result.StatusCode, Is.Zero);
        Assert.That(result.DramaList, Is.Not.Empty);
        Assert.That(result.DramaList, Has.Count.LessThanOrEqualTo(20));
        Assert.That(result.Cursor, Is.Not.Null.And.Not.Empty);

        Assert.Multiple(() =>
        {
            foreach (var drama in result.DramaList)
            {
                Assert.That(drama.DramaId, Is.Not.Null.And.Not.Empty);
                Assert.That(drama.DramaName, Is.Not.Null.And.Not.Empty);
                Assert.That(drama.NumVideos, Is.GreaterThan(0), $"drama {drama.DramaId}");
                Assert.That(drama.Cover?.UrlList, Is.Not.Null.And.Not.Empty, $"drama {drama.DramaId}");
                Assert.That(drama.ZoomCover, Is.Not.Empty, $"drama {drama.DramaId}");
            }
        });
    }

    [Test]
    public async Task GetDramaList_NextCursor_ReturnsDifferentPage()
    {
        var account = await TestAccounts.LoadWithCookieAsync();

        var first = await _tikTokApi.GetDramaListAsync(account.Cookie, _secUid, count: 5, cursor: "0");
        Assume.That(first.HasMore, Is.True, "Kênh không đủ drama để kiểm tra phân trang.");

        var second = await _tikTokApi.GetDramaListAsync(account.Cookie, _secUid, count: 5, cursor: first.Cursor!);

        Assert.That(second.DramaList, Is.Not.Empty);
        Assert.That(
            second.DramaList.Select(d => d.DramaId).Intersect(first.DramaList.Select(d => d.DramaId)),
            Is.Empty,
            "Trang 2 không được trùng drama của trang 1.");
    }
}
