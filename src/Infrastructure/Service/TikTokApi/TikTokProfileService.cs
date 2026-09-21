using System.Text.Json;
using System.Text.RegularExpressions;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Service.TikTokApi;

namespace SurveillanceCameras.Infrastructure.Service.TikTokApi;

public sealed partial class TikTokProfileService : ITikTokProfileService, IAsyncDisposable
{
    private const string DataSelector = "script#__UNIVERSAL_DATA_FOR_REHYDRATION__";

    // Mã trạng thái TikTok đặt trong "webapp.user-detail" khi kênh không tồn tại.
    private const int UserNotFoundStatusCode = 10221;

    private readonly TikTokApiOptions _options;
    private readonly SemaphoreSlim _browserLock = new(1, 1);
    private readonly SemaphoreSlim _pageSlots;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public TikTokProfileService(IOptions<TikTokApiOptions> options)
    {
        _options = options.Value;
        _pageSlots = new SemaphoreSlim(Math.Max(1, _options.MaxConcurrentBrowserPages));
    }

    public async Task<TikTokUserProfileDto> GetUserProfileAsync(
        string? cookie, string handle, CancellationToken cancellationToken = default)
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
