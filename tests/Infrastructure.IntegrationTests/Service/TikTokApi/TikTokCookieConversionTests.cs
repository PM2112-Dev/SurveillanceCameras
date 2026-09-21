using SurveillanceCameras.Infrastructure.Service.TikTokApi;

namespace SurveillanceCameras.Infrastructure.IntegrationTests.Service.TikTokApi;

/// <summary>Kiểm tra chuyển đổi cookie (không cần mạng hay database).</summary>
[TestFixture]
public class TikTokCookieConversionTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(2_000_000_000);

    private const string StoredJson = """
        [
          {"Name":"sessionid","Value":"abc","Domain":".tiktok.com","Path":"/","Expires":2100000000,"HttpOnly":true,"Secure":true,"SameSite":"None"},
          {"Name":"session_only","Value":"x","Domain":".tiktok.com","Path":"/","Expires":-1,"HttpOnly":false,"Secure":false,"SameSite":"Lax"},
          {"Name":"expired","Value":"old","Domain":".tiktok.com","Path":"/","Expires":1900000000,"HttpOnly":false,"Secure":false,"SameSite":"Lax"},
          {"Name":"bad","Value":"line\r\nInjected: 1","Domain":".tiktok.com","Path":"/","Expires":-1}
        ]
        """;

    [Test]
    public void Header_FromJson_SkipsExpiredAndControlCharacters()
    {
        var header = TikTokCookieHeader.Build(StoredJson, Now);

        Assert.That(header, Is.EqualTo("sessionid=abc; session_only=x"));
    }

    [TestCase("a=1; b=2", "a=1; b=2")]
    [TestCase("Cookie: a=1; b=2", "a=1; b=2")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void Header_FromRawString_PassesThrough(string? stored, string expected)
    {
        Assert.That(TikTokCookieHeader.Build(stored, Now), Is.EqualTo(expected));
    }

    [Test]
    public void Playwright_FromJson_KeepsAttributesAndSkipsExpired()
    {
        var cookies = TikTokBrowserCookies.ToPlaywright(StoredJson, "https://www.tiktok.com", Now);

        Assert.That(cookies.Select(c => c.Name), Is.EqualTo(new[] { "sessionid", "session_only", "bad" }));
        var session = cookies[0];
        Assert.Multiple(() =>
        {
            Assert.That(session.Domain, Is.EqualTo(".tiktok.com"));
            Assert.That(session.HttpOnly, Is.True);
            Assert.That(session.Secure, Is.True);
            Assert.That(session.Expires, Is.EqualTo(2100000000f));
            Assert.That(cookies[1].Expires, Is.Null, "Cookie phiên không được đặt Expires.");
        });
    }

    [Test]
    public void Playwright_FromRawString_UsesBaseUrl()
    {
        var cookies = TikTokBrowserCookies.ToPlaywright("a=1; b=2", "https://www.tiktok.com", Now);

        Assert.That(cookies, Has.Count.EqualTo(2));
        Assert.That(cookies.All(c => c.Url == "https://www.tiktok.com"), Is.True);
    }
}
