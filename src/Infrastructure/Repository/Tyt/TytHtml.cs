using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using SurveillanceCameras.Application.Repository.Tyt;

namespace SurveillanceCameras.Infrastructure.Repository.Tyt;

/// <summary>Đọc các trang quản lý truyện của TYT (HTML render sẵn ở server) và dựng dữ liệu gửi lên form.</summary>
public static partial class TytHtml
{
    [GeneratedRegex("/mystory/([0-9a-f]{24})(?:/|$|\\?)", RegexOptions.IgnoreCase)]
    private static partial Regex StoryIdInUrl();

    [GeneratedRegex("/the-loai/([a-z0-9-]+)(?:/|$|\\?|#)", RegexOptions.IgnoreCase)]
    private static partial Regex GenreInUrl();

    // Link trang sửa truyện: đúng "/mystory/{id}/", không phải "/mystory/{id}/chapters/"...
    [GeneratedRegex("/mystory/([0-9a-f]{24})/?$", RegexOptions.IgnoreCase)]
    private static partial Regex EditLink();

    /// <summary>Id truyện trong URL dạng <c>/mystory/{id}/…</c>.</summary>
    public static string? StoryIdFromUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        var match = StoryIdInUrl().Match(url);
        return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
    }

    /// <summary>Thể loại trên trang /the-loai: các liên kết <c>/the-loai/{slug}/…</c> (bỏ liên kết trang gốc), không trùng slug.</summary>
    public static IReadOnlyList<TytGenreDto> ParseGenres(string html)
    {
        var genres = new List<TytGenreDto>();
        var seen = new HashSet<string>();

        foreach (var link in Parse(html).QuerySelectorAll("a[href]"))
        {
            var match = GenreInUrl().Match(link.GetAttribute("href") ?? "");
            var title = Clean(link.TextContent);
            if (match.Success && title.Length > 0 && seen.Add(match.Groups[1].Value.ToLowerInvariant()))
                genres.Add(new TytGenreDto(match.Groups[1].Value.ToLowerInvariant(), title));
        }

        return genres;
    }

    /// <summary>Id chương (24 ký tự hex) trong đường dẫn chuyển hướng sau khi thêm chương; <c>null</c> nếu không có.</summary>
    public static string? ChapterIdFromUrl(string? url, string storyId)
    {
        if (string.IsNullOrEmpty(url)) return null;
        var match = Regex.Match(url, $"/{storyId}/chapters/([0-9a-fA-F]{{24}})", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
    }

    /// <summary>Phản hồi JSON của form thêm chương: <c>status</c>, <c>message</c> và đường dẫn chuyển hướng (TYT viết sai chính tả <c>rediret_url</c>).</summary>
    public static (bool Success, string? Message, string? RedirectUrl) ParseChapterResult(string body)
    {
        var (success, message) = ParseAjaxResult(body);
        try
        {
            using var json = JsonDocument.Parse(body);
            foreach (var name in new[] { "rediret_url", "redirect_url" })
            {
                if (json.RootElement.TryGetProperty(name, out var url) && url.ValueKind == JsonValueKind.String)
                    return (success, message, url.GetString());
            }
        }
        catch (JsonException)
        {
            // Không phải JSON: ParseAjaxResult đã báo không thành công.
        }

        return (success, message, null);
    }

    /// <summary>
    /// Giá trị của lựa chọn "Công Khai" trong ô chế độ hiển thị của trang Xuất bản (giá trị do TYT cấp, không cố định 0/1).
    /// <c>null</c> nếu trang không có ô này (giao diện đổi).
    /// </summary>
    public static string? PublicVisibilityValue(string html)
    {
        var select = Parse(html).QuerySelector("select#published, select[name=published]");
        var option = select?.QuerySelectorAll("option")
            .FirstOrDefault(o => NormalizeTitle(o.TextContent).Contains("công khai"));
        return option?.GetAttribute("value");
    }

    /// <summary>Lựa chọn đang chọn ở ô chế độ hiển thị của trang Xuất bản là "Công Khai".</summary>
    public static bool IsPublishedPublic(string html)
    {
        var select = Parse(html).QuerySelector("select#published, select[name=published]");
        var selected = select?.QuerySelectorAll("option").FirstOrDefault(o => o.HasAttribute("selected"))
                       ?? select?.QuerySelectorAll("option").FirstOrDefault();
        return selected is not null && NormalizeTitle(selected.TextContent).Contains("công khai");
    }

    /// <summary>Chương trong trang danh sách chương: các liên kết <c>…/chapters/{id}/edit</c>; số chương lấy ở ô đầu của dòng.</summary>
    public static IReadOnlyList<TytChapterItemDto> ParseChapterList(string html)
    {
        var chapters = new List<TytChapterItemDto>();
        var seen = new HashSet<string>();

        foreach (var link in Parse(html).QuerySelectorAll("a[href]"))
        {
            var match = Regex.Match(link.GetAttribute("href") ?? "", "/chapters/([0-9a-zA-Z]+)/edit", RegexOptions.IgnoreCase);
            if (!match.Success || !seen.Add(match.Groups[1].Value)) continue;

            var row = link.Closest("tr") ?? link.Closest("li") ?? link.ParentElement;
            var firstCell = row?.QuerySelector("td, th, .chapter-number")?.TextContent;
            var number = Regex.Match(firstCell ?? "", @"\d+") is { Success: true } n ? int.Parse(n.Value) : (int?)null;
            chapters.Add(new TytChapterItemDto(match.Groups[1].Value.ToLowerInvariant(), number, Clean(link.TextContent)));
        }

        return chapters;
    }

    /// <summary>
    /// Trang Nhận Thưởng của truyện. Chưa đủ điều kiện thì trang chỉ báo "Không đủ điều kiện mở nhận thưởng" (chưa có số liệu);
    /// đủ điều kiện thì đọc các ô thống kê (Tổng, Đang Có, Đang Rút, Đã Thanh Toán, Bởi Quảng Cáo, Số Lượt QC) — cách đọc theo
    /// công cụ StoriesAuto.
    /// </summary>
    public static TytEarningDto ParseEarning(string html)
    {
        var document = Parse(html);
        var text = Clean(document.Body?.TextContent);
        var locked = text.Contains("không đủ điều kiện", StringComparison.OrdinalIgnoreCase);

        var stats = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var box in document.QuerySelectorAll(".form-group.row .alert .form-group"))
        {
            var label = Clean(box.QuerySelector("span")?.TextContent).TrimEnd(':').Trim();
            var digits = new string(Clean(box.QuerySelector(".text-primary")?.TextContent).Where(char.IsDigit).ToArray());
            if (label.Length > 0 && long.TryParse(digits, out var value)) stats[label] = value;
        }

        long? Get(string label) => stats.TryGetValue(label, out var v) ? v : null;
        return new TytEarningDto(!locked, Get("Tổng"), Get("Đang Có"), Get("Đang Rút"), Get("Đã Thanh Toán"), Get("Bởi Quảng Cáo"), Get("Số Lượt QC"));
    }

    /// <summary>Trang có nút đăng xuất nghĩa là đang đăng nhập.</summary>
    public static bool IsLoggedIn(string html) => html.Contains("/user/logout", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<TytStoryItemDto> ParseStoryList(string html)
    {
        var document = Parse(html);
        var items = new List<TytStoryItemDto>();
        var seen = new HashSet<string>();

        // Mỗi truyện là một .list-group-item có link "/mystory/{id}/" (link tên truyện và nút "Sửa Truyện").
        foreach (var link in document.QuerySelectorAll("a[href*='/mystory/']"))
        {
            var match = EditLink().Match(link.GetAttribute("href") ?? "");
            if (!match.Success) continue;

            var id = match.Groups[1].Value.ToLowerInvariant();
            if (!seen.Add(id)) continue;

            var row = link.Closest(".list-group-item") ?? link.ParentElement!;
            var title = row.QuerySelectorAll("a[href*='/mystory/']")
                .Select(a => Clean(a.TextContent))
                .FirstOrDefault(text => text.Length > 0);
            var cover = row.QuerySelector("img")?.GetAttribute("src");

            items.Add(new TytStoryItemDto
            {
                Id = id,
                Title = title,
                CoverUrl = string.IsNullOrWhiteSpace(cover) ? null : cover,
                IsPublic = row.QuerySelector(".bi-eye-slash, [title='Chưa công khai']") is null
            });
        }

        return items;
    }

    public static TytStoryDto ParseStory(string html, string storyId)
    {
        var document = Parse(html);

        if (document.QuerySelector("#update_story") is null && document.QuerySelector("input#title") is null)
            throw new FormatException("Trang không có form sửa truyện.");

        string? Value(string selector) => Clean(document.QuerySelector<IHtmlInputElement>(selector)?.Value) is { Length: > 0 } v ? v : null;
        string? Selected(string selector) => document.QuerySelector<IHtmlSelectElement>(selector)
            ?.Options.FirstOrDefault(o => o.IsSelected)?.Value;

        return new TytStoryDto
        {
            Id = storyId,
            Title = Value("input#title"),
            Author = Value("input#author"),
            CoverUrl = Value("input#cover"),
            DescriptionHtml = document.QuerySelector("#story-desc")?.InnerHtml.Trim() is { Length: > 0 } desc ? desc : null,
            Genres = SplitGenres(Value("input#genre")),
            Status = Selected("select#status") switch
            {
                "1" => TytStoryStatus.Full,
                "2" => TytStoryStatus.Dropped,
                _ => TytStoryStatus.Ongoing
            },
            Type = int.TryParse(Selected("select#type"), out var type) && Enum.IsDefined(typeof(TytStoryType), type)
                ? (TytStoryType)type
                : TytStoryType.None
        };
    }

    /// <summary>Thông báo lỗi TYT hiện trên trang (vd. sau khi gửi form thêm truyện không hợp lệ).</summary>
    public static string? ErrorMessage(string html)
    {
        var document = Parse(html);
        var messages = document.QuerySelectorAll(".invalid-feedback, .alert-danger, .text-danger, .error")
            .Select(e => Clean(e.TextContent))
            .Where(text => text.Length > 0)
            .Distinct()
            .ToList();
        return messages.Count == 0 ? null : string.Join(" ", messages);
    }

    /// <summary>Kết quả JSON của các thao tác ajax (<c>{ status, message }</c>).</summary>
    public static (bool Success, string? Message) ParseAjaxResult(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            var status = root.TryGetProperty("status", out var s) && s.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.String => s.GetString() is "true" or "1",
                JsonValueKind.Number => s.GetInt32() == 1,
                _ => false
            };
            var message = root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
                ? Clean(Parse(m.GetString()!).Body?.TextContent)
                : root.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null;
            return (status, message);
        }
        catch (JsonException)
        {
            return (false, null);
        }
    }

    /// <summary>Giới thiệu dạng văn bản (mỗi dòng một đoạn) → HTML cho trình soạn thảo của TYT.</summary>
    /// <summary>Nội dung chương gửi lên TYT: giữ thứ tự dòng, mỗi xuống dòng thành <c>&lt;br&gt;</c> (cách của công cụ đăng StoriesAuto đang chạy ổn với TYT).</summary>
    public static string ChapterContentToHtml(string? text)
        => string.IsNullOrWhiteSpace(text) ? string.Empty : EscapeHtml(text.Replace("\r\n", "\n").Trim()).Replace("\n", "<br>");

    public static string DescriptionToHtml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var html = new StringBuilder();
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            var paragraph = line.Trim();
            if (paragraph.Length > 0) html.Append("<p>").Append(EscapeHtml(paragraph)).Append("</p>");
        }

        return html.ToString();
    }

    /// <summary>Giá trị form: "" = đang ra, "1" = full, "2" = drop.</summary>
    public static string StatusValue(TytStoryStatus status) => status == TytStoryStatus.Ongoing ? "" : ((int)status).ToString();

    /// <summary>Danh sách thể loại dạng "A,B,C" như TYTTag.getData của trang.</summary>
    public static string GenresValue(IEnumerable<string> genres)
        => string.Join(',', genres.Select(g => Clean(g).Replace(",", " ")).Where(g => g.Length > 0).Distinct());

    // Chỉ escape ký tự đặc biệt của HTML — WebUtility.HtmlEncode đổi cả chữ có dấu thành "&#242;".
    private static string EscapeHtml(string text)
        => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    public static string NormalizeTitle(string? title) => Clean(title).ToLowerInvariant();

    private static IReadOnlyList<string> SplitGenres(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',').Select(Clean).Where(g => g.Length > 0).ToList();

    private static string Clean(string? text) => Regex.Replace(text ?? "", @"\s+", " ").Trim();

    private static IDocument Parse(string html) => new HtmlParser().ParseDocument(html);
}
