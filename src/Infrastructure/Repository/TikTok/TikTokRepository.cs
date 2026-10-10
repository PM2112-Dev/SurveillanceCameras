using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Models.TikTok;
using SurveillanceCameras.Application.Repository.TikTok;
using SurveillanceCameras.Infrastructure.Repository.AccountSession;

namespace SurveillanceCameras.Infrastructure.Repository.TikTok;

public sealed partial class TikTokRepository : ITikTokRepository, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string DataSelector = "script#__UNIVERSAL_DATA_FOR_REHYDRATION__";

    // Mã trạng thái TikTok đặt trong "webapp.user-detail" khi kênh không tồn tại.
    private const int UserNotFoundStatusCode = 10221;

    private readonly TikTokApiOptions _options;
    private readonly SemaphoreSlim _browserLock = new(1, 1);
    private readonly SemaphoreSlim _pageSlots;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly HttpClient _httpClient;

    public TikTokRepository(IOptions<TikTokApiOptions> options, HttpClient httpClient)
    {
        _options = options.Value;
        _pageSlots = new SemaphoreSlim(Math.Max(1, _options.MaxConcurrentBrowserPages));
        _httpClient = httpClient;
    }

    public async Task<TikTokUserProfileDto> FetchUserProfile(string? cookie, string? handle, CancellationToken cancellationToken = default)
    {
        var name = handle?.Trim().TrimStart('@') ?? string.Empty;
        if (!HandlePattern().IsMatch(name))
        {
            throw new ArgumentException("Handle TikTok không hợp lệ.", nameof(handle));
        }

        await _pageSlots.WaitAsync(cancellationToken);
        try
        {
            var browser = await GetBrowserAsync(cancellationToken);
            var timeoutMs = _options.TimeoutSeconds * 1000f;

            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = _options.UserAgent,
                Locale = _options.Language,
            });

            var cookies = TikTokBrowserCookies.ToPlaywright(cookie, _options.BaseUrl);
            if (cookies.Count > 0) await context.AddCookiesAsync(cookies);

            // Ảnh/video/font không cần cho việc đọc dữ liệu: bỏ để trang tải nhanh hơn.
            await context.RouteAsync("**/*", route => route.Request.ResourceType is "image" or "media" or "font"
                ? route.AbortAsync()
                : route.ContinueAsync());

            var page = await context.NewPageAsync();
            cancellationToken.ThrowIfCancellationRequested();

            await page.GotoAsync($"{_options.BaseUrl.TrimEnd('/')}/@{name}",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = timeoutMs });

            // Lần tải đầu là trang thử thách WAF ("Please wait…"); JavaScript của nó tự giải rồi tải lại
            // trang thật, nên chỉ cần chờ phần dữ liệu của trang thật xuất hiện.
            await page.WaitForSelectorAsync(DataSelector,
                new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached, Timeout = timeoutMs });

            var json = await page.EvalOnSelectorAsync<string>(DataSelector, "e => e.textContent");

            return Parse(json, name);
        }
        catch (PlaywrightException ex) when (ex.GetType().Name == "TimeoutException")
        {
            throw new TikTokApiException(
                $"Quá thời gian ({_options.TimeoutSeconds}s) chờ trang profile TikTok (có thể bị WAF chặn).", ex);
        }
        catch (PlaywrightException ex)
        {
            throw new TikTokApiException("Lỗi khi mở Chrome headless để lấy profile TikTok: " + ex.Message, ex);
        }
        finally
        {
            _pageSlots.Release();
        }
    }

    public async Task<DramaListDto> FetchDramaList(string? cookie, string secUid, int count = 20, string cursor = "0",
        CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(secUid);

        var query = new (string Key, string Value)[]
        {
            ("aid", "1180"),
            ("app_language", _options.Language),
            ("app_name", "tiktok_web"),
            ("channel", "tiktok_web"),
            ("count", count.ToString()),
            ("coverFormat", "2"),
            ("cursor", cursor),
            ("device_platform", "web_pc"),
            ("from_page", "user"),
            ("language", _options.Language),
            ("priority_region", _options.Region),
            ("region", _options.Region),
            ("secUid", secUid),
        };
        var result = await GetJsonAsync<DramaListDto>("api/drama/user/drama_list/", query, cookie, cancellationToken);
        if (result.StatusCode != 0)
        {
            throw new TikTokApiException(
                $"TikTok báo lỗi (statusCode={result.StatusCode}): {result.StatusMessage}".TrimEnd(' ', ':'));
        }

        return result;
    }

    public async Task<DramaEpisodeListDto> FetchDramaEpisodes(string? cookie, string dramaId, int count = 30,
        string cursor = "0", CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(dramaId);

        var query = new (string Key, string Value)[]
        {
            ("aid", "1180"),
            ("app_language", _options.Language),
            ("app_name", "tiktok_web"),
            ("channel", "tiktok_web"),
            ("count", count.ToString()),
            ("cursor", cursor),
            ("device_platform", "web_pc"),
            ("dramaID", dramaId),
            ("language", _options.Language),
            ("priority_region", _options.Region),
            ("region", _options.Region),
        };

        var result = await GetJsonAsync<DramaEpisodeListDto>("api/drama/episode/item_list/", query, cookie, cancellationToken);
        if (result.StatusCode != 0)
        {
            throw new TikTokApiException(
                $"TikTok báo lỗi (statusCode={result.StatusCode}): {result.StatusMessage}".TrimEnd(' ', ':'));
        }

        return result;
    }

    /// <summary>Gọi API web của TikTok (GET, JSON) với cookie tuỳ chọn.</summary>
    private async Task<T> GetJsonAsync<T>(string path, IEnumerable<(string Key, string Value)> query, string? cookie,
        CancellationToken cancellationToken) where T : class
    {
        var url = path + "?" + string.Join('&', query.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        request.Headers.Referrer = new Uri(_httpClient.BaseAddress!, "/");

        var cookieHeader = StoredCookieHeader.Build(cookie);
        if (cookieHeader.Length > 0)
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        // Mọi lỗi mạng/hết giờ, kể cả TimeoutRejectedException của lớp resilience (Polly).
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TikTokApiException("Không kết nối được tới TikTok: " + ex.Message, ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new TikTokApiException($"TikTok trả về HTTP {(int)response.StatusCode} ({response.StatusCode}).");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(body))
            {
                throw new TikTokApiException("TikTok trả về phản hồi rỗng (request bị chặn hoặc cookie đã hết hạn).");
            }

            try
            {
                return JsonSerializer.Deserialize<T>(body, JsonOptions)
                       ?? throw new TikTokApiException("Phản hồi của TikTok rỗng.");
            }
            catch (JsonException ex)
            {
                throw new TikTokApiException("Phản hồi của TikTok không đúng định dạng JSON mong đợi.", ex);
            }
        }
    }

    private static TikTokUserProfileDto Parse(string? json, string handle)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new TikTokApiException("Trang profile TikTok không có dữ liệu.");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new TikTokApiException("Dữ liệu trang profile TikTok không phải JSON hợp lệ.", ex);
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("__DEFAULT_SCOPE__", out var scope)
                || !scope.TryGetProperty("webapp.user-detail", out var detail))
            {
                throw new TikTokApiException("Trang TikTok không có mục user-detail (TikTok đổi cấu trúc hoặc đang chặn).");
            }

            var statusCode = detail.TryGetProperty("statusCode", out var sc) && sc.TryGetInt32(out var code) ? code : 0;
            if (statusCode == UserNotFoundStatusCode)
            {
                throw new NotFoundException(handle, "TikTok user");
            }

            if (statusCode != 0)
            {
                throw new TikTokApiException($"TikTok báo lỗi khi lấy kênh @{handle} (statusCode={statusCode}).");
            }

            if (!detail.TryGetProperty("userInfo", out var info) || !info.TryGetProperty("user", out var user))
            {
                throw new NotFoundException(handle, "TikTok user");
            }

            info.TryGetProperty("stats", out var stats);

            return new TikTokUserProfileDto
            {
                UserId = ReadString(user, "id"),
                UniqueId = ReadString(user, "uniqueId"),
                SecUid = ReadString(user, "secUid"),
                Nickname = ReadString(user, "nickname"),
                Signature = ReadString(user, "signature"),
                AvatarUrl = ReadString(user, "avatarLarger"),
                Verified = user.TryGetProperty("verified", out var verified) && verified.ValueKind == JsonValueKind.True,
                FollowerCount = ReadLong(stats, "followerCount"),
                FollowingCount = ReadLong(stats, "followingCount"),
                HeartCount = ReadLong(stats, "heartCount"),
                VideoCount = ReadLong(stats, "videoCount"),
            };
        }
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    // TikTok lúc trả số, lúc trả chuỗi số tuỳ trường.
    private static long ReadLong(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value)) return 0;

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out var n) => n,
            JsonValueKind.String when long.TryParse(value.GetString(), out var n) => n,
            _ => 0
        };
    }

    private async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken)
    {
        await _browserLock.WaitAsync(cancellationToken);
        try
        {
            if (_browser is { IsConnected: true }) return _browser;

            _playwright ??= await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Channel = string.IsNullOrWhiteSpace(_options.BrowserChannel) ? null : _options.BrowserChannel,
                // Giảm dấu hiệu "trình duyệt bị điều khiển tự động".
                IgnoreDefaultArgs = ["--enable-automation"],
                Args = ["--disable-blink-features=AutomationControlled"],
            });

            return _browser;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
        _browserLock.Dispose();
        _pageSlots.Dispose();
    }

    [GeneratedRegex("^[A-Za-z0-9._]{1,24}$")]
    private static partial Regex HandlePattern();
}

