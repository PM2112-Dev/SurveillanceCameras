using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.AccountSession;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Infrastructure.Repository.AccountSession;

public sealed class AccountSessionManagerRepository : IAccountSessionManagerRepository, IAsyncDisposable
{
    private readonly AccountSessionOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccountSessionManagerRepository> _logger;

    private readonly ConcurrentDictionary<int, Session> _sessions = new();
    private readonly ConcurrentDictionary<int, AccountSessionStatus> _lastKnown = new();
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private IPlaywright? _playwright;

    public AccountSessionManagerRepository(
        IOptions<AccountSessionOptions> options,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<AccountSessionManagerRepository> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public AccountSessionStatus GetStatus(int accountId)
    {
        if (_sessions.TryGetValue(accountId, out var session))
        {
            return new AccountSessionStatus(accountId, session.State, session.CookieUpdatedAt);
        }

        return _lastKnown.TryGetValue(accountId, out var last)
            ? last with { State = AccountSessionState.Stopped }
            : new AccountSessionStatus(accountId, AccountSessionState.Stopped);
    }

    public async Task StartAsync(int accountId, AccountLoginTarget target, CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (_sessions.ContainsKey(accountId)) return;

            _playwright ??= await Playwright.CreateAsync();

            var profileDir = Path.GetFullPath(Path.Combine(_options.ProfileDirectory, accountId.ToString()));
            Directory.CreateDirectory(profileDir);

            // Giảm dấu hiệu "trình duyệt bị điều khiển tự động" để trang đỡ bắt xác minh.
            List<string> args = ["--disable-blink-features=AutomationControlled"];
            if (_options.RemoteDebuggingPortBase > 0)
            {
                args.Add($"--remote-debugging-port={_options.RemoteDebuggingPortBase + accountId}");
            }

            var context = await _playwright.Chromium.LaunchPersistentContextAsync(profileDir,
                new BrowserTypeLaunchPersistentContextOptions
                {
                    Headless = _options.Headless,
                    Channel = string.IsNullOrWhiteSpace(_options.Channel) ? null : _options.Channel,
                    ViewportSize = _options.Headless ? null : ViewportSize.NoViewport,
                    IgnoreDefaultArgs = ["--enable-automation"],
                    Args = args,
                });

            var session = new Session(accountId, context, target, CookieDomainOf(target.LoginUrl));
            _sessions[accountId] = session;

            // Người dùng tự đóng cửa sổ Chrome -> dọn phiên để có thể Start lại.
            context.Close += (_, _) => _ = CleanupAsync(session);
            // Mỗi response có thể mang Set-Cookie -> đánh thức vòng theo dõi, không cần tải lại trang.
            context.Response += (_, _) => session.Signal();

            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
            try
            {
                await page.GotoAsync(target.LoginUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            }
            catch (Exception ex)
            {
                // Vẫn giữ cửa sổ mở để người dùng tự xử lý (mạng chậm, captcha...).
                _logger.LogWarning(ex, "Account {AccountId}: navigate to {Url} failed", accountId, target.LoginUrl);
            }

            session.Monitor = Task.Run(() => MonitorAsync(session));
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <summary>Thời gian tối đa cho một request (kể cả khi trang đang chờ Cloudflare xác minh).</summary>
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(60);

    private const string FetchScript = """
        async ({ url, form, ajax, timeoutMs }) => {
            const controller = new AbortController();
            const timer = setTimeout(() => controller.abort(), timeoutMs);
            try {
                const init = { credentials: 'include', signal: controller.signal, headers: {} };
                if (ajax) init.headers['X-Requested-With'] = 'XMLHttpRequest';
                if (form) {
                    init.method = 'POST';
                    init.body = new URLSearchParams(form);
                }
                const response = await fetch(url, init);
                return { status: response.status, url: response.url, body: await response.text() };
            } finally {
                clearTimeout(timer);
            }
        }
        """;

    public async Task<BrowserFetchResponse> FetchAsync(int accountId, AccountLoginTarget target, BrowserFetchRequest request,
        CancellationToken cancellationToken = default)
    {
        var domain = CookieDomainOf(target.LoginUrl);
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !BelongsTo(uri.Host, domain))
        {
            // Phiên mang cookie đăng nhập: không cho gửi tới host khác.
            throw new BrowserFetchException($"URL {request.Url} không thuộc trang của tài khoản ({domain}).");
        }

        await StartAsync(accountId, target, cancellationToken);
        if (!_sessions.TryGetValue(accountId, out var session))
            throw new BrowserFetchException("Không mở được trình duyệt của tài khoản.");

        try
        {
            var page = await GetWorkerPageAsync(session, uri, cancellationToken);
            var response = await RunFetchAsync(page, request, cancellationToken);

            if (IsChallenge(response))
            {
                // Cloudflare xác minh lại giữa chừng: tải lại trang để trình duyệt vượt xác minh rồi gửi lại.
                await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                await WaitForChallengeAsync(page);
                response = await RunFetchAsync(page, request, cancellationToken);
            }

            return response;
        }
        catch (PlaywrightException ex)
        {
            throw new BrowserFetchException($"Trình duyệt của tài khoản lỗi khi gửi request tới {uri}: {ex.Message}", ex);
        }
        catch (TimeoutException ex)
        {
            throw new BrowserFetchException($"Hết giờ chờ {uri} (trang có thể đang bắt xác minh Cloudflare).", ex);
        }
    }

    /// <summary>
    /// Trang riêng để gửi request (không đụng trang người dùng đang đăng nhập), luôn đứng ở origin của trang để
    /// <c>fetch</c> gửi kèm cookie.
    /// </summary>
    private static async Task<IPage> GetWorkerPageAsync(Session session, Uri uri, CancellationToken cancellationToken)
    {
        var origin = uri.GetLeftPart(UriPartial.Authority);

        await session.WorkerLock.WaitAsync(cancellationToken);
        try
        {
            if (session.WorkerPage is { IsClosed: false } existing
                && existing.Url.StartsWith(origin + "/", StringComparison.OrdinalIgnoreCase))
            {
                return existing;
            }

            var page = session.WorkerPage is { IsClosed: false } reused ? reused : await session.Context.NewPageAsync();
            session.WorkerPage = page;
            await page.GotoAsync(origin + "/", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await WaitForChallengeAsync(page);
            return page;
        }
        finally
        {
            session.WorkerLock.Release();
        }
    }

    private static Task WaitForChallengeAsync(IPage page)
        => page.WaitForFunctionAsync("() => !document.title.includes('Just a moment')", null,
            new PageWaitForFunctionOptions { Timeout = (float)FetchTimeout.TotalMilliseconds });

    private static async Task<BrowserFetchResponse> RunFetchAsync(IPage page, BrowserFetchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await page.EvaluateAsync<JsonElement>(FetchScript, new
        {
            url = request.Url,
            form = request.FormBody?.Select(pair => new[] { pair.Key, pair.Value }).ToArray(),
            ajax = request.Ajax,
            timeoutMs = FetchTimeout.TotalMilliseconds
        }).WaitAsync(FetchTimeout + TimeSpan.FromSeconds(5), cancellationToken);

        return new BrowserFetchResponse(
            result.GetProperty("status").GetInt32(),
            result.GetProperty("url").GetString() ?? request.Url,
            result.GetProperty("body").GetString() ?? string.Empty);
    }

    private static bool IsChallenge(BrowserFetchResponse response)
        => response.StatusCode is 403 or 503
           && (response.Body.Contains("challenge-platform", StringComparison.Ordinal)
               || response.Body.Contains("Just a moment", StringComparison.Ordinal));

    public async Task StopAsync(int accountId)
    {
        if (!_sessions.TryGetValue(accountId, out var session)) return;

        await CleanupAsync(session);
    }

    private async Task MonitorAsync(Session session)
    {
        var ct = session.Cts.Token;
        string? lastHash = null;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                lastHash = await SyncCookiesAsync(session, lastHash, ct);

                // Chờ tới chu kỳ poll, hoặc sớm hơn nếu có response mới.
                await session.WaitForSignalAsync(TimeSpan.FromSeconds(_options.PollIntervalSeconds), ct);
                await Task.Delay(TimeSpan.FromSeconds(_options.DebounceSeconds), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (PlaywrightException ex)
            {
                // Context đã đóng (người dùng tắt Chrome) -> handler Close sẽ dọn.
                _logger.LogInformation(ex, "Account {AccountId}: browser no longer available", session.AccountId);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account {AccountId}: cookie sync failed", session.AccountId);
                try { await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), ct); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private async Task<string?> SyncCookiesAsync(Session session, string? lastHash, CancellationToken ct)
    {
        var cookies = (await session.Context.CookiesAsync())
            .Where(c => BelongsTo(c.Domain, session.CookieDomain))
            .OrderBy(c => c.Domain).ThenBy(c => c.Path).ThenBy(c => c.Name)
            .ToList();

        var loginCookie = session.Target.LoginCookieName;
        var loggedIn = string.IsNullOrWhiteSpace(loginCookie)
            ? cookies.Count > 0
            : cookies.Any(c => c.Name == loginCookie && !string.IsNullOrEmpty(c.Value));

        if (!loggedIn)
        {
            // Không ghi đè cookie đã lưu trong DB: cookie cũ vẫn là bản cuối cùng còn dùng được.
            session.State = session.EverLoggedIn ? AccountSessionState.LoggedOut : AccountSessionState.WaitingForLogin;
            return lastHash;
        }

        session.EverLoggedIn = true;
        session.State = AccountSessionState.Active;

        var json = JsonSerializer.Serialize(cookies.Select(c => new
        {
            c.Name,
            c.Value,
            c.Domain,
            c.Path,
            c.Expires,
            c.HttpOnly,
            c.Secure,
            SameSite = c.SameSite.ToString(),
        }));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        if (hash == lastHash) return lastHash;

        var now = _timeProvider.GetUtcNow();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // ExecuteUpdate bỏ qua interceptor nên không ghi đè LastModifiedBy (không có user trong tiến trình nền).
        var updated = await db.Accounts
            .Where(a => a.Id == session.AccountId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(a => a.Cookie, json)
                .SetProperty(a => a.LastModified, now), ct);

        if (updated == 0)
        {
            _logger.LogWarning("Account {AccountId} no longer exists, stopping session", session.AccountId);
            _ = CleanupAsync(session);
            return hash;
        }

        session.CookieUpdatedAt = now;
        _logger.LogInformation("Account {AccountId}: cookie updated ({Count} cookies)", session.AccountId, cookies.Count);
        return hash;
    }

    private async Task CleanupAsync(Session session)
    {
        if (!_sessions.TryRemove(session.AccountId, out _)) return;

        _lastKnown[session.AccountId] = new AccountSessionStatus(
            session.AccountId, AccountSessionState.Stopped, session.CookieUpdatedAt);

        await session.Cts.CancelAsync();
        try
        {
            await session.Context.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Account {AccountId}: error closing browser context", session.AccountId);
        }

        if (session.Monitor is not null && session.Monitor != Task.CompletedTask)
        {
            try { await session.Monitor; }
            catch (Exception) { /* đã log trong MonitorAsync */ }
        }

        session.Cts.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var session in _sessions.Values.ToList())
        {
            await CleanupAsync(session);
        }

        _playwright?.Dispose();
        _lifecycleLock.Dispose();
    }

    /// <summary>Domain lấy cookie: host của trang đăng nhập, bỏ "www." để lấy cả cookie của subdomain khác.</summary>
    private static string CookieDomainOf(string loginUrl)
    {
        var host = new Uri(loginUrl).Host;
        return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
    }

    // Khớp đúng domain hoặc subdomain của nó — "eviltiktok.com" không được tính là "tiktok.com".
    private static bool BelongsTo(string cookieDomain, string domain)
    {
        var value = cookieDomain.TrimStart('.');
        return value.Equals(domain, StringComparison.OrdinalIgnoreCase)
               || value.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class Session(int accountId, IBrowserContext context, AccountLoginTarget target, string cookieDomain)
    {
        private readonly SemaphoreSlim _signal = new(0, 1);

        public int AccountId { get; } = accountId;
        public IBrowserContext Context { get; } = context;
        public AccountLoginTarget Target { get; } = target;
        public string CookieDomain { get; } = cookieDomain;
        public CancellationTokenSource Cts { get; } = new();
        public Task? Monitor { get; set; }

        /// <summary>Trang dùng cho <see cref="FetchAsync"/>; khoá để các request song song không cùng mở trang.</summary>
        public IPage? WorkerPage { get; set; }
        public SemaphoreSlim WorkerLock { get; } = new(1, 1);

        public volatile AccountSessionState State = AccountSessionState.WaitingForLogin;
        public bool EverLoggedIn;
        public DateTimeOffset? CookieUpdatedAt;

        public void Signal()
        {
            try { _signal.Release(); }
            catch (SemaphoreFullException) { /* đã có tín hiệu chờ xử lý */ }
            catch (ObjectDisposedException) { }
        }

        public async Task WaitForSignalAsync(TimeSpan timeout, CancellationToken ct) =>
            await _signal.WaitAsync(timeout, ct);
    }
}
