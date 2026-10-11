using NUnit.Framework;
using Shouldly;
using System.Text.Json;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Models.TikTok;
using SurveillanceCameras.Infrastructure.Repository.TikTok;

namespace SurveillanceCameras.Application.UnitTests.Movies;

public class TikTokVideoPageTests
{
    private static string Page(string videoDetail)
        => "<html><head><script id=\"__UNIVERSAL_DATA_FOR_REHYDRATION__\" type=\"application/json\">"
           + "{\"__DEFAULT_SCOPE__\":{\"webapp.video-detail\":" + videoDetail + "}}</script></head></html>";

    [Test]
    public void SessionCookies_SendsTheStoredAccountCookies_ToTikTokAndItsVideoHosts()
    {
        var stored = "[{\"name\":\"sessionid\",\"value\":\"abc\",\"domain\":\".tiktok.com\"},{\"name\":\"old\",\"value\":\"x\",\"expires\":1}]";

        var cookies = TikTokRepository.SessionCookies(stored, new Uri("https://www.tiktok.com/@kenh/video/1"));

        cookies.GetCookieHeader(new Uri("https://www.tiktok.com/@kenh/video/1")).ShouldBe("sessionid=abc");
        cookies.GetCookieHeader(new Uri("https://v16-webapp-prime.tiktok.com/video/a/")).ShouldBe("sessionid=abc");
        cookies.GetCookieHeader(new Uri("https://example.com/")).ShouldBeEmpty();
        TikTokRepository.SessionCookies(null, new Uri("https://www.tiktok.com/")).Count.ShouldBe(0);
        TikTokRepository.SessionCookies("a=1; b=2", new Uri("https://www.tiktok.com/")).Count.ShouldBe(2);
    }

    // Dạng phản hồi của /api/drama/episode/item_list/ khi đã đăng nhập (khảo sát 2026-10-11).
    [Test]
    public void DownloadUrls_PrefersTheBestH264Rendition_ThenTheDefaultLink_ThenOtherCodecs()
    {
        var video = JsonSerializer.Deserialize<DramaEpisodeVideoDto>("""
            {"duration":99,"playAddr":"https://v45.tiktokcdn.com/default","bitrateInfo":[
              {"CodecType":"bytevc1","Bitrate":971746,"PlayAddr":{"UrlList":["https://v16.tiktok.com/hevc-hi"]}},
              {"CodecType":"h264","Bitrate":"704587","PlayAddr":{"UrlList":["https://v16.tiktok.com/h264-lo"]}},
              {"CodecType":"h264","Bitrate":1002054,"PlayAddr":{"UrlList":["https://v16.tiktok.com/h264-hi","https://v19.tiktok.com/h264-hi","http://khong-an-toan/x"]}}]}
            """)!;

        video.DownloadUrls().ShouldBe([
            "https://v16.tiktok.com/h264-hi", "https://v19.tiktok.com/h264-hi", "https://v16.tiktok.com/h264-lo",
            "https://v45.tiktokcdn.com/default", "https://v16.tiktok.com/hevc-hi"
        ]);
        // Chưa đăng nhập: TikTok không trả link nào.
        JsonSerializer.Deserialize<DramaEpisodeVideoDto>("""{"duration":49,"cover":"https://img/x.jpg"}""")!.DownloadUrls().ShouldBeEmpty();
    }

    [Test]
    public void ParseVideoPlayUrl_ReturnsPlayAddr()
    {
        var html = Page("{\"statusCode\":0,\"itemInfo\":{\"itemStruct\":{\"video\":{\"playAddr\":\"https://v16-webapp.tiktok.com/video/a/?a=1988\\u0026br=2\"}}}}");

        TikTokRepository.ParseVideoPlayUrl(html).AbsoluteUri.ShouldBe("https://v16-webapp.tiktok.com/video/a/?a=1988&br=2");
    }

    [Test]
    public void ParseVideoPlayUrl_FallsBackToFirstBitrate_WhenPlayAddrIsEmpty()
    {
        var html = Page("{\"statusCode\":0,\"itemInfo\":{\"itemStruct\":{\"video\":{\"playAddr\":\"\",\"bitrateInfo\":["
                        + "{\"PlayAddr\":{\"UrlList\":[\"https://v16.tiktok.com/hd.mp4\",\"https://v19.tiktok.com/hd.mp4\"]}},"
                        + "{\"PlayAddr\":{\"UrlList\":[\"https://v16.tiktok.com/sd.mp4\"]}}]}}}}");

        TikTokRepository.ParseVideoPlayUrl(html).AbsoluteUri.ShouldBe("https://v16.tiktok.com/hd.mp4");
    }

    // Tập drama: TikTok trả trang bình thường nhưng bỏ trống link file (khảo sát 2026-10-11).
    [Test]
    public void ParseVideoPlayUrl_Throws_WhenTikTokWithholdsTheFile()
    {
        var html = Page("{\"statusCode\":0,\"itemInfo\":{\"itemStruct\":{\"video\":{\"playAddr\":\"\",\"downloadAddr\":\"\"}}}}");

        Should.Throw<TikTokVideoUnavailableException>(() => TikTokRepository.ParseVideoPlayUrl(html)).Message.ShouldContain("không cấp link tải");
    }

    [Test]
    public void ParseVideoPlayUrl_Throws_WhenVideoIsUnavailable_OrPageIsBlocked()
    {
        Should.Throw<TikTokVideoUnavailableException>(() => TikTokRepository.ParseVideoPlayUrl(Page("{\"statusCode\":10204,\"statusMsg\":\"item doesn't exist\"}")))
            .Message.ShouldContain("10204");
        Should.Throw<TikTokBlockedException>(() => TikTokRepository.ParseVideoPlayUrl("<html>Please wait...</html>"))
            .Message.ShouldContain("không có dữ liệu");
    }
}
