using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Repository.AccountSession;

public record AccountSessionStatus(
    int AccountId,
    AccountSessionState State,
    DateTimeOffset? CookieUpdatedAt = null);

/// <param name="LoginUrl">Trang mở trong Chrome để người dùng đăng nhập (<c>AccountType.BaseUrl</c>).</param>
/// <param name="LoginCookieName">
/// Cookie có mặt nghĩa là đã đăng nhập. Bỏ trống thì mọi thay đổi cookie của trang đều được lưu
/// (kể cả khi chưa/không còn đăng nhập).
/// </param>
public record AccountLoginTarget(string LoginUrl, string? LoginCookieName);

/// <summary>Request gửi bằng <c>fetch</c> trong trang của phiên Chrome (cùng cookie, đã qua Cloudflare).</summary>
/// <param name="Url">URL tuyệt đối, phải cùng host với trang đăng nhập của loại tài khoản.</param>
/// <param name="FormBody">Có thì gửi POST dạng <c>application/x-www-form-urlencoded</c>; <c>null</c> thì GET.</param>
/// <param name="Ajax">Thêm header <c>X-Requested-With: XMLHttpRequest</c> như request jQuery của trang.</param>
public record BrowserFetchRequest(string Url, IReadOnlyList<KeyValuePair<string, string>>? FormBody = null, bool Ajax = false);

/// <param name="Url">URL cuối cùng sau khi theo các lần chuyển hướng.</param>
public record BrowserFetchResponse(int StatusCode, string Url, string Body);

/// <summary>
/// Mở Chrome trên server cho người dùng đăng nhập vào trang của loại tài khoản, rồi theo dõi và lưu cookie của trang đó
/// vào <c>Account.Cookie</c> mỗi khi thay đổi.
/// </summary>
public interface IAccountSessionManagerRepository
{
    Task StartAsync(int accountId, AccountLoginTarget target, CancellationToken cancellationToken = default);

    /// <summary>Đóng trình duyệt. Profile được giữ lại nên lần sau vẫn còn đăng nhập.</summary>
    Task StopAsync(int accountId);

    AccountSessionStatus GetStatus(int accountId);

    /// <summary>
    /// Gửi request bằng phiên Chrome của tài khoản — dùng cho trang chặn request không phải trình duyệt (Cloudflare).
    /// Phiên chưa mở thì tự mở. Lỗi mạng/trình duyệt ném <see cref="Common.Exceptions.BrowserFetchException"/>.
    /// </summary>
    Task<BrowserFetchResponse> FetchAsync(int accountId, AccountLoginTarget target, BrowserFetchRequest request,
        CancellationToken cancellationToken = default);
}
