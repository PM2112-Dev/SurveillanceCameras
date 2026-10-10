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
