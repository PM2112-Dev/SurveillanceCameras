using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Repository.AccountSession;
using SurveillanceCameras.Application.Repository.Tyt;
using SurveillanceCameras.Infrastructure.Repository.Tyt;

namespace SurveillanceCameras.Infrastructure.IntegrationTests.Repository.Tyt;

/// <summary>HTML mẫu rút gọn từ trang thật của tytnovel.info (khảo sát 2026-09-28).</summary>
public class TytRepositoryTests
{
    private const string Base = "https://tytnovel.info";
    private const string Id1 = "69e495b5b709112fc40d96f2";
    private const string Id2 = "69e4abd2b709112fc40d99b0";
    private const string Logout = "<a href=\"https://tytnovel.info/user/logout\">Thoát</a>";

    private static readonly TytContext Context = new(7, Base + "/", null);

    private static string Row(string id, string title, bool isPublic = false) => $"""
        <div class="list-group-item d-flex align-items-center gap-3 py-3">
            <img class="object-fit-cover" src="" alt="">
            <div class="flex-grow-1">
                <a href="/mystory/{id}/" class="text-decoration-none fw-medium text-body">{title}</a>
                {(isPublic ? "" : "<i class=\"bi bi-eye-slash text-secondary\" title=\"Chưa công khai\"></i>")}
            </div>
            <div class="d-flex gap-2 flex-shrink-0">
                <a title="Sửa Truyện" href="/mystory/{id}/" class="btn"><i class="bi bi-pencil-square"></i></a>
                <a title="Danh Sách Chương" href="/mystory/{id}/chapters/" class="btn"><i class="bi bi-list"></i></a>
                <a title="Xem Trước" href="/truyen/{id}" class="btn"><i class="bi bi-eye"></i></a>
            </div>
        </div>
        """;

    private static string ListPage(params string[] rows)
        => $"<html><body>{Logout}<a href=\"/mystory/new/\">Thêm</a><div class=\"list-group my-3\">{string.Concat(rows)}</div></body></html>";

    private static string EditPage(string id, string status = "", string type = "0", string genre = "", string desc = "") => $"""
        <html><body>{Logout}
        <a class="story-menu-link active" href="/mystory/{id}/">Thông Tin</a>
        <input type="text" name="title" id="title" value="Chiến Chùy Chi Hắc Hoàng Đế">
        <div id="title_error" class="text-danger"></div>
        <input type="text" name="author" id="author" value="Lạt Tiêu Sao Ác Ma">
        <input type="text" name="cover" id="cover" value="">
        <select id="type" name="type">
            <option value="0" {(type == "0" ? "selected" : "")}></option>
            <option value="1" {(type == "1" ? "selected" : "")}>Dịch / Edit</option>
            <option value="3" {(type == "3" ? "selected" : "")}>Convert / QT</option>
        </select>
        <input type="hidden" name="genre" id="genre" value="{genre}">
        <select name="status" id="status">
            <option value="" {(status == "" ? "selected" : "")}>Đang ra</option>
            <option value="1" {(status == "1" ? "selected" : "")}>Hoàn - Full</option>
            <option value="2" {(status == "2" ? "selected" : "")}>Dừng - Drop</option>
        </select>
        <div id="story-desc" class="richeditor-content" contenteditable="true">{desc}</div>
        <span id="update_story" data-id="{id}"></span>
        </body></html>
        """;

    [Test]
    public void ParseStoryList_ReadsIdTitleAndVisibility_IgnoringChapterLinks()
    {
        var items = TytHtml.ParseStoryList(ListPage(Row(Id1, "Chiến Chùy"), Row(Id2, " Truyện  B ", isPublic: true)));

        Assert.That(items.Select(x => x.Id), Is.EqualTo(new[] { Id1, Id2 }));
        Assert.That(items[1].Title, Is.EqualTo("Truyện B"));
        Assert.That(items[0].IsPublic, Is.False);
        Assert.That(items[1].IsPublic, Is.True);
        Assert.That(items[0].CoverUrl, Is.Null);
    }

    [Test]
    public void ParseStory_ReadsCurrentFormValues()
    {
        var story = TytHtml.ParseStory(EditPage(Id1, status: "1", type: "3", genre: "Ngôn Tình,Cổ Đại", desc: "<p>Văn án</p>"), Id1);

        Assert.That(story.Title, Is.EqualTo("Chiến Chùy Chi Hắc Hoàng Đế"));
        Assert.That(story.Author, Is.EqualTo("Lạt Tiêu Sao Ác Ma"));
        Assert.That(story.CoverUrl, Is.Null);
        Assert.That(story.Genres, Is.EqualTo(new[] { "Ngôn Tình", "Cổ Đại" }));
        Assert.That(story.Status, Is.EqualTo(TytStoryStatus.Full));
        Assert.That(story.Type, Is.EqualTo(TytStoryType.Convert));
        Assert.That(story.DescriptionHtml, Is.EqualTo("<p>Văn án</p>"));
    }

    [Test]
    public void DescriptionToHtml_EncodesAndSplitsParagraphs()
        => Assert.That(TytHtml.DescriptionToHtml("Dòng <1>\r\n\n Dòng 2 "), Is.EqualTo("<p>Dòng &lt;1&gt;</p><p>Dòng 2</p>"));

    [Test]
    public async Task GetStories_FollowsPagesUntilNoNewItems_OldestFirst()
    {
        var browser = new FakeBrowser()
            .On($"{Base}/mystory", ListPage(Row(Id2, "B")))
            .On($"{Base}/mystory?page=2", ListPage(Row(Id1, "A")))
            .On($"{Base}/mystory?page=3", ListPage());

        var stories = await new TytRepository(browser).GetStoriesAsync(Context);

        Assert.That(stories.Select(x => x.Id), Is.EqualTo(new[] { Id1, Id2 }));
    }

    [Test]
    public async Task GetStoryId_DuplicateTitles_ReturnsNewest()
    {
        var browser = new FakeBrowser()
            .On($"{Base}/mystory", ListPage(Row(Id1, "Chiến Chùy"), Row(Id2, "chiến  chùy"), Row("69e4b2c2c4ea784a544dd4a1", "Khác")))
            .On($"{Base}/mystory?page=2", ListPage());

        var repository = new TytRepository(browser);

        Assert.That(await repository.GetStoryIdAsync(Context, "Chiến Chùy"), Is.EqualTo(Id2));
        Assert.That(await repository.GetStoryIdAsync(Context, "Không có"), Is.Null);
    }

    [Test]
    public async Task Create_RedirectToStoryPage_ReturnsNewId()
    {
        var browser = new FakeBrowser()
            .On($"{Base}/mystory", ListPage())
            .On($"{Base}/mystory/new", EditPage(Id2), finalUrl: $"{Base}/mystory/{Id2}/");

        var id = await new TytRepository(browser).CreateStoryAsync(Context, new TytCreateStoryRequest { Title = "Truyện Mới", Author = "Tác Giả" });

        Assert.That(id, Is.EqualTo(Id2));
        var post = browser.Requests.Single(r => r.FormBody is not null);
        Assert.That(post.FormBody, Is.EquivalentTo(new Dictionary<string, string>
        {
            ["title"] = "Truyện Mới", ["author"] = "Tác Giả", ["agree"] = "1"
        }));
    }

    [Test]
    public async Task Create_NoRedirect_FindsNewStoryNotInPreviousList()
    {
        var calls = 0;
        var browser = new FakeBrowser()
            // Trước khi tạo đã có một truyện trùng tên (Id1); sau khi tạo có thêm Id2.
            .On($"{Base}/mystory", () => ++calls == 1 ? ListPage(Row(Id1, "Truyện Mới")) : ListPage(Row(Id1, "Truyện Mới"), Row(Id2, "Truyện Mới")))
            .On($"{Base}/mystory?page=2", ListPage())
            .On($"{Base}/mystory/new", ListPage(), finalUrl: $"{Base}/mystory");

        var id = await new TytRepository(browser).CreateStoryAsync(Context, new TytCreateStoryRequest { Title = "Truyện Mới" });

        Assert.That(id, Is.EqualTo(Id2));
    }

    [Test]
    public async Task Update_SendsChangedFields_AndKeepsTheRest()
    {
        var browser = new FakeBrowser()
            .On($"{Base}/mystory/{Id1}/", EditPage(Id1, status: "1", genre: "Ngôn Tình", desc: "<p>Cũ</p>"))
            .On($"{Base}/mystory/{Id1}/update", """{"status":true,"message":"Cập nhật thành công"}""");

        await new TytRepository(browser).UpdateStoryAsync(Context, Id1, new TytUpdateStoryRequest
        {
            Title = "Tên Mới", Genres = ["Ngôn Tình", "Cổ Đại"], Type = TytStoryType.Translated
        });

        var post = browser.Requests.Single(r => r.FormBody is not null);
        Assert.That(post.Ajax, Is.True);
        Assert.That(post.FormBody, Is.EquivalentTo(new Dictionary<string, string>
        {
            ["title"] = "Tên Mới",
            ["cover"] = "",
            ["desc"] = "<p>Cũ</p>",
            ["genre"] = "Ngôn Tình,Cổ Đại",
            ["author"] = "Lạt Tiêu Sao Ác Ma",
            ["status"] = "1",
            ["type"] = "1"
        }));
    }

    [Test]
    public void Update_TytRejects_ThrowsWithMessage()
    {
        var browser = new FakeBrowser()
            .On($"{Base}/mystory/{Id1}/", EditPage(Id1))
            .On($"{Base}/mystory/{Id1}/update", """{"status":false,"message":"Tên truyện đã tồn tại"}""");

        var ex = Assert.ThrowsAsync<TytException>(() =>
            new TytRepository(browser).UpdateStoryAsync(Context, Id1, new TytUpdateStoryRequest { Author = "X" }));

        Assert.That(ex!.Message, Does.Contain("Tên truyện đã tồn tại"));
    }

    [Test]
    public void NotLoggedIn_ThrowsClearError()
    {
        var browser = new FakeBrowser().On($"{Base}/mystory", "<html><body><a href=\"/user/login\">Đăng nhập</a></body></html>");

        var ex = Assert.ThrowsAsync<TytException>(() => new TytRepository(browser).GetStoriesAsync(Context));

        Assert.That(ex!.Message, Does.Contain("chưa đăng nhập"));
    }

    [Test]
    public void InvalidStoryId_IsRejectedBeforeAnyRequest()
    {
        var browser = new FakeBrowser();

        Assert.ThrowsAsync<TytException>(() => new TytRepository(browser).GetStoryAsync(Context, "../user/logout"));
        Assert.That(browser.Requests, Is.Empty);
    }

    private sealed class FakeBrowser : IAccountSessionManagerRepository
    {
        private readonly Dictionary<string, (Func<string> Body, string? FinalUrl)> _routes = new();

        public List<(string Url, Dictionary<string, string>? FormBody, bool Ajax)> Requests { get; } = [];

        public FakeBrowser On(string url, string body, string? finalUrl = null) => On(url, () => body, finalUrl);

        public FakeBrowser On(string url, Func<string> body, string? finalUrl = null)
        {
            _routes[url] = (body, finalUrl);
            return this;
        }

        public Task<BrowserFetchResponse> FetchAsync(int accountId, AccountLoginTarget target, BrowserFetchRequest request,
            CancellationToken cancellationToken = default)
        {
            Assert.That(accountId, Is.EqualTo(7));
            Requests.Add((request.Url, request.FormBody?.ToDictionary(p => p.Key, p => p.Value), request.Ajax));

            return Task.FromResult(_routes.TryGetValue(request.Url, out var route)
                ? new BrowserFetchResponse(200, route.FinalUrl ?? request.Url, route.Body())
                : new BrowserFetchResponse(404, request.Url, ""));
        }

        public Task StartAsync(int accountId, AccountLoginTarget target, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync(int accountId) => Task.CompletedTask;

        public AccountSessionStatus GetStatus(int accountId) => throw new NotSupportedException();
    }
}
