using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using SurveillanceCameras.Application.Service.TikTokApi;
using SurveillanceCameras.Application.TikTokApi;
using SurveillanceCameras.Infrastructure.Service.TikTokApi;

namespace SurveillanceCameras.Infrastructure.IntegrationTests.Service.TikTokApi;

/// <summary>
/// Test tích hợp thật: cookie từ database thật + Chrome headless thật + TikTok thật.
/// Cần Postgres, Google Chrome đã cài (hoặc <c>TikTokApi:BrowserChannel</c> trống + <c>playwright install chromium</c>) và mạng.
/// Biến môi trường tuỳ chọn: <c>TIKTOK_TEST_HANDLE</c> (mặc định stardusttv_vietnam).
/// </summary>
[TestFixture]
[Category("Integration")]
public class TikTokProfileServiceIntegrationTests
{
    private const string DefaultHandle = "stardusttv_vietnam";

    // secUid công khai của @stardusttv_vietnam (đã biết từ API drama_list) để kiểm tra handle -> secUid đúng.
    private const string KnownSecUid =
        "MS4wLjABAAAA7s0_xRU3BcsfFJV3Suu_z3E9lDorIFsiJ11lOCJfGHX6QGTMpxnRQ1bftpjlgFfe";

    private ServiceProvider _services = null!;
    private ITikTokProfileService _profiles = null!;
    private ITikTokApiService _tikTokApi = null!;
    private string _handle = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        _services = new ServiceCollection()
            .AddTikTokApi(TestEnvironment.Configuration)
            .BuildServiceProvider();

        _profiles = _services.GetRequiredService<ITikTokProfileService>();
        _tikTokApi = _services.GetRequiredService<ITikTokApiService>();
        _handle = Environment.GetEnvironmentVariable("TIKTOK_TEST_HANDLE") is { Length: > 0 } h ? h : DefaultHandle;
    }

    [OneTimeTearDown]
    public async Task TearDown() => await _services.DisposeAsync();

    [Test]
    public async Task GetUserProfile_RealHandle_WithCookieFromDatabase_ReturnsProfile()
    {
        var account = await TestAccounts.LoadWithCookieAsync();

        var profile = await _profiles.GetUserProfileAsync(account.Cookie, _handle);

        TestContext.Out.WriteLine(
            $"@{profile.UniqueId} ({profile.Nickname}) — {profile.FollowerCount} follower, {profile.VideoCount} video, verified={profile.Verified}");
        TestContext.Out.WriteLine($"secUid={profile.SecUid}");

        Assert.Multiple(() =>
        {
            Assert.That(profile.UniqueId, Is.EqualTo(_handle).IgnoreCase);
            Assert.That(profile.UserId, Is.Not.Null.And.Not.Empty);
            Assert.That(profile.SecUid, Does.StartWith("MS4w"));
            Assert.That(profile.Nickname, Is.Not.Null.And.Not.Empty);
            Assert.That(profile.FollowerCount, Is.GreaterThan(0));
            Assert.That(profile.VideoCount, Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task GetUserProfile_WithAtSign_Works()
    {
        var account = await TestAccounts.LoadWithCookieAsync();

        var profile = await _profiles.GetUserProfileAsync(account.Cookie, "@" + _handle);

        Assert.That(profile.UniqueId, Is.EqualTo(_handle).IgnoreCase);
    }

    [Test]
    public async Task GetUserProfile_ThenDramaList_EndToEnd()
    {
        var account = await TestAccounts.LoadWithCookieAsync();

        // Đi từ handle -> secUid (Chrome headless) -> danh sách drama (HTTP) bằng cùng cookie trong DB.
        var profile = await _profiles.GetUserProfileAsync(account.Cookie, DefaultHandle);
        Assert.That(profile.SecUid, Is.EqualTo(KnownSecUid), "secUid lấy từ trang profile phải khớp secUid đã biết.");

        var dramas = await _tikTokApi.GetDramaListAsync(account.Cookie, profile.SecUid!, count: 5);

        TestContext.Out.WriteLine($"@{DefaultHandle}: {dramas.DramaList.Count} drama đầu tiên, hasMore={dramas.HasMore}");
        Assert.That(dramas.DramaList, Is.Not.Empty);
    }

    [Test]
    public async Task GetUserProfile_UnknownHandle_ThrowsNotFound()
    {
        var account = await TestAccounts.LoadWithCookieAsync();

        Assert.ThrowsAsync<NotFoundException>(
            async () => await _profiles.GetUserProfileAsync(account.Cookie, "no_such_user_x9f3a1c7e"));
    }

    [TestCase("")]
    [TestCase("has space")]
    [TestCase("a/b")]
    [TestCase("this_handle_is_way_too_long_for_tiktok")]
    public void GetUserProfile_InvalidHandle_ThrowsBeforeOpeningBrowser(string handle)
    {
        Assert.ThrowsAsync<ArgumentException>(async () => await _profiles.GetUserProfileAsync(null, handle));
    }
}
