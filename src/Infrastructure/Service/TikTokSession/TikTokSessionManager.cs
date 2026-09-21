using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Service.TikTokSession;

namespace SurveillanceCameras.Infrastructure.Service.TikTokSession;

public sealed class TikTokSessionManager : ITikTokSessionManager, IAsyncDisposable
{
    private readonly TikTokSessionOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TikTokSessionManager> _logger;

    private readonly ConcurrentDictionary<int, Session> _sessions = new();
    private readonly ConcurrentDictionary<int, TikTokSessionStatus> _lastKnown = new();
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private IPlaywright? _playwright;

    public TikTokSessionManager(
        IOptions<TikTokSessionOptions> options,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<TikTokSessionManager> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public TikTokSessionStatus GetStatus(int accountId)
    {
        if (_sessions.TryGetValue(accountId, out var session))
        {
            return new TikTokSessionStatus(accountId, session.State, session.CookieUpdatedAt);
        }

        return _lastKnown.TryGetValue(accountId, out var last)
            ? last with { State = TikTokSessionState.Stopped }
            : new TikTokSessionStatus(accountId, TikTokSessionState.Stopped);
    }

    public async Task StartAsync(int accountId, CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (_sessions.ContainsKey(accountId)) return;

            _playwright ??= await Playwright.CreateAsync();

            var profileDir = Path.GetFullPath(Path.Combine(_options.ProfileDirectory, accountId.ToString()));
            Directory.CreateDirectory(profileDir);

            var context = await _playwright.Chromium.LaunchPersistentContextAsync(profileDir,
                new BrowserTypeLaunchPersistentContextOptions
                {
                    Headless = _options.Headless,
                    Channel = string.IsNullOrWhiteSpace(_options.Channel) ? null : _options.Channel,
                    ViewportSize = _options.Headless ? null : ViewportSize.NoViewport,
                    // Giảm dấu hiệu "trình duyệt bị điều khiển tự động" để TikTok đỡ bắt xác minh.
                    IgnoreDefaultArgs = ["--enable-automation"],
                    Args = ["--disable-blink-features=AutomationControlled"],
                });

            var session = new Session(accountId, context);
            _sessions[accountId] = session;

            // Người dùng tự đóng cửa sổ Chrome -> dọn phiên để có thể Start lại.
            context.Close += (_, _) => _ = CleanupAsync(session);
            // Mỗi response có thể mang Set-Cookie -> đánh thức vòng theo dõi, không cần tải lại trang.
            context.Response += (_, _) => session.Signal();

            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
            try
            {
                await page.GotoAsync(_options.LoginUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            }
            catch (Exception ex)
            {
                // Vẫn giữ cửa sổ mở để người dùng tự xử lý (mạng chậm, captcha...).
                _logger.LogWarning(ex, "TikTok account {AccountId}: navigate to {Url} failed", accountId, _options.LoginUrl);
            }

            session.Monitor = Task.Run(() => MonitorAsync(session));
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

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
                _logger.LogInformation(ex, "TikTok account {AccountId}: browser no longer available", session.AccountId);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TikTok account {AccountId}: cookie sync failed", session.AccountId);
                try { await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), ct); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private async Task<string?> SyncCookiesAsync(Session session, string? lastHash, CancellationToken ct)
    {
        var domain = _options.CookieDomain.TrimStart('.');
        var cookies = (await session.Context.CookiesAsync())
            .Where(c => c.Domain.TrimStart('.').EndsWith(domain, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Domain).ThenBy(c => c.Path).ThenBy(c => c.Name)
            .ToList();

        var loggedIn = cookies.Any(c =>
            c.Name == _options.SessionCookieName && !string.IsNullOrEmpty(c.Value));

        if (!loggedIn)
        {
            // Không ghi đè cookie đã lưu trong DB: cookie cũ vẫn là bản cuối cùng còn dùng được.
            session.State = session.EverLoggedIn ? TikTokSessionState.LoggedOut : TikTokSessionState.WaitingForLogin;
            return lastHash;
        }

        session.EverLoggedIn = true;
        session.State = TikTokSessionState.Active;

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
            _logger.LogWarning("TikTok account {AccountId} no longer exists, stopping session", session.AccountId);
            _ = CleanupAsync(session);
            return hash;
        }

        session.CookieUpdatedAt = now;
        _logger.LogInformation("TikTok account {AccountId}: cookie updated ({Count} cookies)", session.AccountId, cookies.Count);
        return hash;
    }

    private async Task CleanupAsync(Session session)
    {
        if (!_sessions.TryRemove(session.AccountId, out _)) return;

        _lastKnown[session.AccountId] = new TikTokSessionStatus(
            session.AccountId, TikTokSessionState.Stopped, session.CookieUpdatedAt);

        await session.Cts.CancelAsync();
        try
        {
            await session.Context.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "TikTok account {AccountId}: error closing browser context", session.AccountId);
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

    private sealed class Session(int accountId, IBrowserContext context)
    {
        private readonly SemaphoreSlim _signal = new(0, 1);

        public int AccountId { get; } = accountId;
        public IBrowserContext Context { get; } = context;
        public CancellationTokenSource Cts { get; } = new();
        public Task? Monitor { get; set; }

        public volatile TikTokSessionState State = TikTokSessionState.WaitingForLogin;
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
