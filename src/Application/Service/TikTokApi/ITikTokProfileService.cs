namespace SurveillanceCameras.Application.Service.TikTokApi;

/// <summary>
/// Lấy thông tin kênh TikTok bằng Chrome headless. Trang profile nằm sau WAF của TikTok
/// (cần chạy JavaScript để giải thử thách) nên không thể gọi bằng HttpClient thường.
/// </summary>
public interface ITikTokProfileService
{
    /// <param name="cookie">Giá trị <c>Account.Cookie</c> (có thể rỗng — trang profile là công khai).</param>
    /// <param name="handle">Handle của kênh, có hoặc không có dấu @.</param>
    /// <exception cref="Ardalis.GuardClauses.NotFoundException">Không có kênh với handle này.</exception>
    /// <exception cref="Common.Exceptions.TikTokApiException">TikTok chặn, quá thời gian, hoặc trang không đúng định dạng.</exception>
    Task<TikTokUserProfileDto> GetUserProfileAsync(
        string? cookie, string handle, CancellationToken cancellationToken = default);
}
