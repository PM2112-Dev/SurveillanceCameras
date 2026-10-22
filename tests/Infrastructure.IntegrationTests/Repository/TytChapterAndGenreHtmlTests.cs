using SurveillanceCameras.Infrastructure.Repository.Tyt;

namespace SurveillanceCameras.Infrastructure.IntegrationTests.Repository;

public class TytChapterAndGenreHtmlTests
{
    private const string StoryId = "6937c842dac07c1fa72c5482";

    [Test]
    public void Genres_AreReadFromTheCategoryLinks_WithoutDuplicatesOrTheIndexLink()
    {
        const string html = """
            <nav><a href="https://tytnovel.info/the-loai">Thể loại</a></nav>
            <main>
              <a href="https://tytnovel.info/the-loai/ngon-tinh/cap-nhat">Ngôn Tình</a>
              <a href="https://tytnovel.info/the-loai/dam-my/cap-nhat"> Đam   Mỹ </a>
              <a href="/the-loai/ngon-tinh/hoan-thanh">Ngôn Tình</a>
              <a href="https://tytnovel.info/the-loai/1v1/cap-nhat">1v1</a>
              <a href="https://tytnovel.info/the-loai#wrap">Đầu trang</a>
              <a href="/truyen/abc">Không phải thể loại</a>
            </main>
            """;

        var genres = TytHtml.ParseGenres(html);

        Assert.That(genres.Select(x => x.Title), Is.EqualTo(new[] { "Ngôn Tình", "Đam Mỹ", "1v1" }));
        Assert.That(genres.Select(x => x.Slug), Is.EqualTo(new[] { "ngon-tinh", "dam-my", "1v1" }));
    }

    [Test]
    public void ChapterResult_ReadsStatusMessageAndTheMisspelledRedirectUrl()
    {
        var ok = TytHtml.ParseChapterResult($$"""{"status":true,"message":"Thêm chương thành công","rediret_url":"https://tytnovel.info/mystory/{{StoryId}}/chapters/6937d000dac07c1fa72c5499"}""");
        Assert.That(ok.Success, Is.True);
        Assert.That(ok.Message, Is.EqualTo("Thêm chương thành công"));
        Assert.That(TytHtml.ChapterIdFromUrl(ok.RedirectUrl, StoryId), Is.EqualTo("6937d000dac07c1fa72c5499"));

        var failed = TytHtml.ParseChapterResult("""{"status":false,"message":"Số chương đã tồn tại"}""");
        Assert.That(failed.Success, Is.False);
        Assert.That(failed.Message, Does.Contain("đã tồn tại"));
        Assert.That(failed.RedirectUrl, Is.Null);

        Assert.That(TytHtml.ChapterIdFromUrl("https://tytnovel.info/mystory/" + StoryId + "/chapters", StoryId), Is.Null);
        Assert.That(TytHtml.ParseChapterResult("<html>lỗi</html>").Success, Is.False);
    }

    [Test]
    public void PublicVisibilityValue_IsReadFromThePublishPage_NotHardCoded()
    {
        const string html = """
            <select id="published" name="published">
              <option value="11" selected>Chỉ Mình Tôi</option>
              <option value="25">Công Khai - Mọi Người</option>
            </select>
            """;

        Assert.That(TytHtml.PublicVisibilityValue(html), Is.EqualTo("25"));
        Assert.That(TytHtml.PublicVisibilityValue("<p>không có ô chọn</p>"), Is.Null);
    }

    [Test]
    public void PublishPage_IsPublic_OnlyWhenTheSelectedOptionIsPublic()
    {
        const string priv = """<select id="published"><option value="11" selected>Chỉ Mình Tôi</option><option value="25">Công Khai - Mọi Người</option></select>""";
        const string pub = """<select id="published"><option value="11">Chỉ Mình Tôi</option><option value="25" selected>Công Khai - Mọi Người</option></select>""";

        Assert.That(TytHtml.IsPublishedPublic(priv), Is.False);
        Assert.That(TytHtml.IsPublishedPublic(pub), Is.True);
        Assert.That(TytHtml.IsPublishedPublic("<p>không có ô chọn</p>"), Is.False);
    }

    [Test]
    public void ChapterList_ReadsIdsAndNumbersFromTheEditLinks()
    {
        const string html = """
            <table><tbody>
              <tr><td>1</td><td><a href="/mystory/6937c842dac07c1fa72c5482/chapters/6937d000dac07c1fa72c5499/edit">Chương mở đầu</a></td><td>Menu</td></tr>
              <tr><td> 12 </td><td><a href="/mystory/6937c842dac07c1fa72c5482/chapters/6937d000dac07c1fa72c54aa/edit">Chương mười hai</a></td><td>Menu</td></tr>
              <tr><td>12</td><td><a href="/mystory/6937c842dac07c1fa72c5482/chapters/6937d000dac07c1fa72c54aa/edit">Trùng</a></td></tr>
            </tbody></table>
            """;

        var chapters = TytHtml.ParseChapterList(html);

        Assert.That(chapters.Select(x => (x.Id, x.Number)), Is.EqualTo(new[]
        {
            ("6937d000dac07c1fa72c5499", (int?)1), ("6937d000dac07c1fa72c54aa", (int?)12)
        }));
    }
}
