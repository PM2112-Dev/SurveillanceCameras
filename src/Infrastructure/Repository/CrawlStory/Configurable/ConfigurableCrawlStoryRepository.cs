using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.Config;
using SurveillanceCameras.Application.Repository.CrawlStory.DTOs;
using SurveillanceCameras.Infrastructure.Repository.AccountSession;

namespace SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;

/// <summary>Lấy dữ liệu truyện theo <see cref="CrawlConfig"/> của nguồn web — dùng chung cho mọi trang khai báo được bằng cấu hình.</summary>
public sealed class ConfigurableCrawlStoryRepository : ICrawlStoryRepository
{
    public const string HttpClientName = "CrawlStory";

    private const int MaxRedirects = 5;

    private readonly HttpClient _httpClient;

    public ConfigurableCrawlStoryRepository(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientName);
    }

    public Task<CrawlPageDto<StoryCrawlDto>> FetchStoryListAsync(CrawlContext context, int page,
        CancellationToken cancellationToken = default)
    {
        var config = Require(context.Config?.StoryList, "storyList");

        return FetchListAsync(context, config, BaseVars(context, context.BaseUrl), page, (item, url) => new StoryCrawlDto
        {
            Title = CrawlSelector.One(item, config.Title, url),
            Link = CrawlSelector.One(item, config.Link, url, isUrl: true),
            Author = CrawlSelector.One(item, config.Author, url),
            CoverUrl = CrawlSelector.One(item, config.Cover, url, isUrl: true),
            Status = CrawlSelector.One(item, config.Status, url),
            ChapterCount = CrawlSelector.Number(item, config.ChapterCount, url)
        }, cancellationToken);
    }

    public async Task<StoryDataDto> FetchStoryAsync(CrawlContext context, string storyUrl,
        CancellationToken cancellationToken = default)
    {
        var config = Require(context.Config?.Story, "story");
        var page = await LoadAsync(context, storyUrl, null, cancellationToken);
        var doc = page.Document;

        return new StoryDataDto
        {
            Url = page.Url.ToString(),
            Title = CrawlSelector.One(doc, config.Title, page.Url),
            Author = CrawlSelector.One(doc, config.Author, page.Url),
            Description = config.Description is null
                ? null
                : string.Join('\n', CrawlSelector.Select(doc, config.Description).SelectMany(CrawlSelector.Paragraphs)),
            CoverUrl = CrawlSelector.One(doc, config.Cover, page.Url, isUrl: true),
            Status = CrawlSelector.One(doc, config.Status, page.Url),
            Genres = CrawlSelector.All(doc, config.Genres, page.Url),
            ChapterCount = CrawlSelector.Number(doc, config.ChapterCount, page.Url)
        };
    }

    public async Task<CrawlPageDto<ChapterDataDto>> FetchChapterListAsync(CrawlContext context, string storyUrl, int page,
        CancellationToken cancellationToken = default)
    {
        var config = Require(context.Config?.ChapterList, "chapterList");
        var vars = BaseVars(context, storyUrl);

        if (config.Vars is { Count: > 0 })
        {
            // Biến (vd. khoá ký request) nằm trong script của trang truyện.
            var storyPage = await LoadAsync(context, storyUrl, null, cancellationToken);
            foreach (var (name, pattern) in config.Vars)
            {
                var match = Regex.Match(storyPage.Html, pattern);
                vars[name] = match.Success
                    ? match.Groups[match.Groups.Count > 1 ? 1 : 0].Value
                    : throw new CrawlException($"Không tìm thấy biến '{name}' trên trang truyện (regex: {pattern}).");
            }
        }

        return await FetchListAsync(context, config, vars, page, (item, url) => new ChapterDataDto
        {
            Title = CrawlSelector.One(item, config.Title, url),
            Link = CrawlSelector.One(item, config.Link, url, isUrl: true)
        }, cancellationToken);
    }

    public async Task<ChapterContentDto> FetchChapterAsync(CrawlContext context, string chapterUrl,
        CancellationToken cancellationToken = default)
    {
        var config = Require(context.Config?.Chapter, "chapter");
        var page = await LoadAsync(context, chapterUrl, null, cancellationToken);

        var content = CrawlSelector.Select(page.Document, config.Content).FirstOrDefault()
                      ?? throw new CrawlException($"Không tìm thấy nội dung chương (selector: {config.Content}).");

        foreach (var selector in config.Remove ?? [])
        {
            foreach (var element in CrawlSelector.Select(content, selector)) element.Remove();
        }

        return new ChapterContentDto
        {
            Url = page.Url.ToString(),
            Title = CrawlSelector.One(page.Document, config.Title, page.Url),
            Paragraphs = CrawlSelector.Paragraphs(content)
        };
    }

    // ── Danh sách có phân trang ───────────────────────────────────────────────

    private async Task<CrawlPageDto<T>> FetchListAsync<T>(CrawlContext context, CrawlListConfig config,
        Dictionary<string, string> vars, int page, Func<IElement, Uri, T> map, CancellationToken cancellationToken)
    {
        if (!config.Reverse)
        {
            var listPage = await LoadListPageAsync(context, config, vars, page, cancellationToken);
            return ToPage(listPage, page, listPage.TotalPages, listPage.HasNextPage, map, reverse: false);
        }

        // Trang nguồn xếp mới nhất trước: trang hiển thị p ứng với trang nguồn (tổng - p + 1).
        var first = await LoadListPageAsync(context, config, vars, 1, cancellationToken);
        if (first.TotalPages is not { } total)
        {
            // Không biết tổng số trang thì chỉ đảo được thứ tự trong từng trang.
            var samePage = page == 1 ? first : await LoadListPageAsync(context, config, vars, page, cancellationToken);
            return ToPage(samePage, page, null, samePage.HasNextPage, map, reverse: true);
        }

        var sourcePage = total - page + 1;
        if (sourcePage < 1) return new CrawlPageDto<T> { Page = page, TotalPages = total };

        var target = sourcePage == 1 ? first : await LoadListPageAsync(context, config, vars, sourcePage, cancellationToken);
        return ToPage(target, page, total, page < total, map, reverse: true);
    }

    private static CrawlPageDto<T> ToPage<T>(ListPage listPage, int page, int? totalPages, bool hasNextPage,
        Func<IElement, Uri, T> map, bool reverse)
    {
        var items = listPage.Items.Select(item => map(item, listPage.Url)).ToList();
        if (reverse) items.Reverse();

        return new CrawlPageDto<T>
        {
            Items = items,
            Page = page,
            TotalPages = totalPages,
            HasNextPage = hasNextPage,
            SourceUrl = listPage.Url.ToString()
        };
    }

    private async Task<ListPage> LoadListPageAsync(CrawlContext context, CrawlListConfig config,
        Dictionary<string, string> vars, int page, CancellationToken cancellationToken)
    {
        var pagination = config.Pagination;
        var pageSize = pagination?.PageSize ?? 0;

        var pageVars = new Dictionary<string, string>(vars, StringComparer.OrdinalIgnoreCase)
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
            ["offset"] = ((page - 1) * pageSize).ToString()
        };

        var urlTemplate = page > 1 && !string.IsNullOrWhiteSpace(pagination?.PageUrl)
            ? pagination.PageUrl
            : config.Request?.Url ?? "{url}";

        var url = CrawlTemplate.Render(urlTemplate, pageVars);
        if (config.Request?.Query is { Count: > 0 } query)
        {
            var queryString = string.Join('&', query.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(CrawlTemplate.Render(pair.Value, pageVars))}"));
            url += (url.Contains('?') ? '&' : '?') + queryString;
        }

        var headers = config.Request?.Headers?.ToDictionary(pair => pair.Key, pair => CrawlTemplate.Render(pair.Value, pageVars));
        var loaded = await LoadAsync(context, url, headers, cancellationToken);

        var items = CrawlSelector.Select(loaded.Document, config.Item);
        var totalPages = TotalPages(loaded, pagination);

        bool hasNextPage;
        if (totalPages is { } total) hasNextPage = page < total;
        else if (!string.IsNullOrWhiteSpace(pagination?.NextPage)) hasNextPage = CrawlSelector.Select(loaded.Document, pagination.NextPage).Count > 0;
        else hasNextPage = pageSize > 0 && items.Count >= pageSize;

        return new ListPage(loaded.Url, items, totalPages, hasNextPage);
    }

    private static int? TotalPages(LoadedPage page, CrawlPaginationConfig? pagination)
    {
        if (string.IsNullOrWhiteSpace(pagination?.LastPage)) return null;

        var numbers = CrawlSelector.All(page.Document, pagination.LastPage, page.Url)
            .Select(CrawlSelector.ParseNumber)
            .OfType<int>()
            .ToList();

        if (numbers.Count == 0) return null;

        var max = numbers.Max();
        if (!pagination.LastPageIsOffset) return max;

        return pagination.PageSize is > 0 and var size
            ? max / size + 1
            : throw new CrawlException("pagination.lastPageIsOffset cần có pagination.pageSize.");
    }

    // ── Tải trang ─────────────────────────────────────────────────────────────

    private async Task<LoadedPage> LoadAsync(CrawlContext context, string url, IDictionary<string, string>? headers,
        CancellationToken cancellationToken)
    {
        var baseUri = new Uri(context.BaseUrl);
        var uri = ToAllowedUri(WithCacheBuster(url, context.Config?.CacheBuster), baseUri);

        for (var redirect = 0; redirect <= MaxRedirects; redirect++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            foreach (var (name, value) in context.Config?.Headers ?? []) request.Headers.TryAddWithoutValidation(name, value);
            foreach (var (name, value) in headers ?? new Dictionary<string, string>()) request.Headers.TryAddWithoutValidation(name, value);
            // Account.Cookie có thể là JSON do phiên Chrome lưu hoặc chuỗi dán tay — đổi về dạng header.
            var cookieHeader = StoredCookieHeader.Build(context.Cookie);
            if (cookieHeader.Length > 0) request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            // Mọi lỗi mạng/hết giờ, kể cả TimeoutRejectedException của lớp resilience (Polly).
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new CrawlException($"Không kết nối được tới {uri}: {ex.Message}", ex);
            }

            using (response)
            {
                // Tự theo redirect để kiểm tra host ở từng bước (redirect có thể trỏ ra ngoài nguồn web).
                if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is { } location)
                {
                    uri = ToAllowedUri(new Uri(uri, location).ToString(), baseUri);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new CrawlException(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.ServiceUnavailable
                        ? $"Trang nguồn chặn request ({(int)response.StatusCode}) tới {uri} — có thể do Cloudflare/cookie hết hạn."
                        : $"Trang nguồn trả về lỗi {(int)response.StatusCode} cho {uri}.");
                }

                var html = await response.Content.ReadAsStringAsync(cancellationToken);
                // Parser tạo mới mỗi lần: repository có thể được gọi song song (vd. đồng bộ nhiều truyện).
                var document = await new HtmlParser().ParseDocumentAsync(html, cancellationToken);
                return new LoadedPage(uri, html, document);
            }
        }

        throw new CrawlException($"Quá nhiều lần chuyển hướng khi tải {url}.");
    }

    private static Uri ToAllowedUri(string url, Uri baseUri)
    {
        if (!Uri.TryCreate(baseUri, url, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new CrawlException($"URL không hợp lệ: {url}");

        if (!string.Equals(uri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase))
            throw new CrawlException($"URL {uri} không thuộc host của nguồn web ({baseUri.Host}).");

        return uri;
    }

    private static Dictionary<string, string> BaseVars(CrawlContext context, string url)
    {
        var baseUri = new Uri(context.BaseUrl);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["url"] = WithoutFragment(url).TrimEnd('/'),
            ["baseUrl"] = context.BaseUrl.TrimEnd('/'),
            ["origin"] = baseUri.GetLeftPart(UriPartial.Authority)
        };
    }

    private static string WithCacheBuster(string url, string? parameter)
    {
        if (string.IsNullOrWhiteSpace(parameter)) return url;

        var fragment = url.IndexOf('#');
        var path = fragment < 0 ? url : url[..fragment];
        var value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return $"{path}{(path.Contains('?') ? '&' : '?')}{Uri.EscapeDataString(parameter)}={value}";
    }

    // Link copy từ trình duyệt hay dính "#..." (vd. "#!"), phải bỏ trước khi ghép thêm path/query.
    private static string WithoutFragment(string url)
    {
        var hash = url.IndexOf('#');
        return hash < 0 ? url : url[..hash];
    }

    private static T Require<T>(T? section, string name) where T : class
        => section ?? throw new CrawlException($"Cấu hình crawl của nguồn web chưa có phần '{name}'.");

    private sealed record LoadedPage(Uri Url, string Html, IDocument Document);

    private sealed record ListPage(Uri Url, IReadOnlyList<IElement> Items, int? TotalPages, bool HasNextPage);
}
