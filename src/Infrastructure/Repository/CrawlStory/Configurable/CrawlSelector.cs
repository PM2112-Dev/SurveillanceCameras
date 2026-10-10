using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using SurveillanceCameras.Application.Common.Exceptions;

namespace SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;

/// <summary>
/// Đọc selector trong cấu hình crawl: <c>"css"</c> lấy text, <c>"css@attr"</c> lấy thuộc tính, <c>"css@html"</c> lấy HTML,
/// css rỗng (vd. <c>"@href"</c>) nghĩa là chính phần tử đang xét.
/// </summary>
public static partial class CrawlSelector
{
    private static readonly HashSet<string> SkippedTags = ["script", "style", "noscript", "template", "meta", "link"];

    private static readonly HashSet<string> BlockTags =
        ["p", "div", "br", "li", "ul", "ol", "h1", "h2", "h3", "h4", "h5", "h6", "section", "article", "blockquote", "pre", "tr", "table"];

    /// <summary>Các phần tử khớp css; css rỗng trả về chính <paramref name="scope"/>.</summary>
    public static IReadOnlyList<IElement> Select(INode scope, string? css)
    {
        if (string.IsNullOrWhiteSpace(css))
        {
            return scope switch
            {
                IElement element => [element],
                IDocument document when document.DocumentElement is not null => [document.DocumentElement],
                _ => []
            };
        }

        if (scope is not IParentNode parent) return [];

        try
        {
            return parent.QuerySelectorAll(css).ToList();
        }
        catch (DomException ex)
        {
            throw new CrawlException($"Selector CSS không hợp lệ '{css}': {ex.Message}", ex);
        }
    }

    /// <summary>Giá trị đầu tiên của selector; <paramref name="isUrl"/> thì đổi sang URL tuyệt đối.</summary>
    public static string? One(INode scope, string? spec, Uri pageUrl, bool isUrl = false)
        => All(scope, spec, pageUrl, isUrl).FirstOrDefault();

    /// <summary>Giá trị (không rỗng, không trùng) của mọi phần tử khớp selector.</summary>
    public static List<string> All(INode scope, string? spec, Uri pageUrl, bool isUrl = false)
    {
        if (spec is null) return [];

        var (css, attr) = Split(spec);

        return Select(scope, css)
            .Select(element => ValueOf(element, attr))
            .Select(value => isUrl ? ToAbsoluteUrl(value, pageUrl) : value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct()
            .ToList();
    }

    /// <summary>Số nguyên đầu tiên trong giá trị selector (vd. "Chương 128" → 128, "1,480" → 1480).</summary>
    public static int? Number(INode scope, string? spec, Uri pageUrl)
        => ParseNumber(One(scope, spec, pageUrl));

    public static int? ParseNumber(string? text)
    {
        var match = text is null ? null : NumberPattern().Match(text);
        return match is { Success: true } && int.TryParse(match.Value.Replace(",", "").Replace(".", ""), out var number)
            ? number
            : null;
    }

    /// <summary>Tách phần tử thành các đoạn văn (theo thẻ khối và &lt;br&gt;), bỏ script/style.</summary>
    public static List<string> Paragraphs(IElement element)
    {
        var text = new StringBuilder();
        Walk(element, text);

        return text.ToString()
            .Split('\n')
            .Select(CollapseWhitespace)
            .Where(line => line.Length > 0)
            .ToList();
    }

    private static void Walk(INode node, StringBuilder text)
    {
        switch (node)
        {
            case IText textNode:
                text.Append(textNode.Data);
                return;
            case IElement element:
                var tag = element.LocalName;
                if (SkippedTags.Contains(tag)) return;

                var isBlock = BlockTags.Contains(tag);
                if (isBlock) text.Append('\n');
                foreach (var child in element.ChildNodes) Walk(child, text);
                if (isBlock) text.Append('\n');
                return;
        }
    }

    private static (string? Css, string? Attr) Split(string spec)
    {
        var at = spec.LastIndexOf('@');
        if (at < 0) return (spec.Trim(), null);

        var attr = spec[(at + 1)..].Trim();
        return (spec[..at].Trim(), attr.Length == 0 ? null : attr);
    }

    private static string? ValueOf(IElement element, string? attr) => attr switch
    {
        null or "text" => CollapseWhitespace(element.TextContent),
        "html" => element.InnerHtml,
        _ => element.GetAttribute(attr)?.Trim()
    };

    private static string? ToAbsoluteUrl(string? value, Uri pageUrl)
    {
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;

        return Uri.TryCreate(pageUrl, value, out var url) ? url.ToString() : null;
    }

    private static string CollapseWhitespace(string text) => Whitespace().Replace(text, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"\d[\d.,]*")]
    private static partial Regex NumberPattern();
}
