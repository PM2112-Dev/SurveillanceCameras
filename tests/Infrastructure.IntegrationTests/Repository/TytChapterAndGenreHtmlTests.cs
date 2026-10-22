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

    [Test]
    public void ChapterContent_KeepsLinesWithBrTags_AndEscapesHtml()
    {
        Assert.That(TytHtml.ChapterContentToHtml("Đoạn một.\r\nĐoạn <hai> & ba.\n\nĐoạn bốn.\n"),
            Is.EqualTo("Đoạn một.<br>Đoạn &lt;hai&gt; &amp; ba.<br><br>Đoạn bốn."));
        Assert.That(TytHtml.ChapterContentToHtml("  \n "), Is.Empty);
    }

    [Test]
    public void Earning_LockedPageHasNoFigures_EligiblePageIsParsedFromTheRealLayout()
    {
        var locked = TytHtml.ParseEarning("<main><h5>Nhận Thưởng</h5><div>Không đủ điều kiện mở nhận thưởng. Vui lòng quay lại sau</div></main>");
        Assert.That(locked.Eligible, Is.False);
        Assert.That(locked.Total, Is.Null);

        // Cấu trúc trang Nhận Thưởng thật (khảo sát 2026-10-22).
        const string html = """
            <select id="earning_ad" name="earning_ad"><option value=""></option><option value="1" selected="">Bật</option><option value="0">Tắt</option></select>
            <div class="row g-3 mb-2">
              <div class="col"><div class="text-uppercase">Số Dư</div><div class="row">
                <div class="card-body p-3"><div class="d-flex"><i class="bi bi-wallet2"></i> Đang Có</div><div class="fs-5 fw-bold text-primary">3,066 đ</div></div>
                <div class="card-body p-3"><div class="d-flex"><i></i> Đang Rút</div><div class="fs-6">0 đ</div></div>
                <div class="card-body p-3"><div class="d-flex"><i></i> Đã Thanh Toán</div><div class="fs-6">0 đ</div></div>
                <div class="card-body p-3"><div class="d-flex"><i></i> Đã Chuyển</div><div class="fs-6">9,990 đ</div></div>
              </div></div>
              <div class="col"><div class="text-uppercase">Nguồn Thu</div><div class="row">
                <div class="card-body p-3"><div class="d-flex"><i></i> Tổng</div><div class="fs-5">13,056 đ</div></div>
                <div class="card-body p-3"><div class="d-flex"><i></i> Bởi Quảng Cáo</div><div class="fs-6">13,056 đ</div><div class="text-secondary small"><i></i> 2,007 lượt</div></div>
                <div class="card-body p-3"><div class="d-flex"><i></i> Bởi Đề Cử</div><div class="fs-6">0 đ</div><div class="text-secondary small"><i></i> 0 lượt</div></div>
              </div></div>
            </div>
            """;
        var earning = TytHtml.ParseEarning(html);

        Assert.That(earning.Eligible, Is.True);
        Assert.That(earning.EarningEnabled, Is.True);
        Assert.That((earning.Total, earning.Available, earning.Withdrawing, earning.Paid, earning.Transferred, earning.FromAds, earning.AdViews),
            Is.EqualTo(((long?)13056, (long?)3066, (long?)0, (long?)0, (long?)9990, (long?)13056, (long?)2007)));

        Assert.That(TytHtml.ParseEarning(html.Replace(" selected=\"\"", "")).EarningEnabled, Is.False);
    }

    [Test]
    public void ChapterList_RealLayout_NumberInTheRowHeader_TitleInTheEditLink()
    {
        const string html = """
            <table><tbody>
              <tr><th scope="row" class="fw-normal"> 146 </th><th><a href="/mystory/6937c842dac07c1fa72c5482/chapters/6937d000dac07c1fa72c5499/edit" class="text-decoration-none fw-normal">Chương 146: Kết</a></th>
                  <th><div class="d-flex gap-2"><a title="Cập Nhật Chương" href="/mystory/6937c842dac07c1fa72c5482/chapters/6937d000dac07c1fa72c5499/edit"><i></i></a><a title="Xem Trước" href="/truyen/x/chuong/y"><i></i></a></div></th></tr>
              <tr><th scope="row"> 145 </th><th><a href="/mystory/6937c842dac07c1fa72c5482/chapters/6937d000dac07c1fa72c54aa/edit">Chương 145: Vô Đề</a></th></tr>
            </tbody></table>
            """;

        var chapters = TytHtml.ParseChapterList(html);

        Assert.That(chapters.Select(x => (x.Id, x.Number, x.Title)), Is.EqualTo(new[]
        {
            ("6937d000dac07c1fa72c5499", (int?)146, "Chương 146: Kết"), ("6937d000dac07c1fa72c54aa", (int?)145, "Chương 145: Vô Đề")
        }));
    }
}
