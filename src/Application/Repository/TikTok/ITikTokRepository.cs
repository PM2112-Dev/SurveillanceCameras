using SurveillanceCameras.Application.Common.Models.TikTok;

namespace SurveillanceCameras.Application.Repository.TikTok;

public interface ITikTokRepository
{
    Task<TikTokUserProfileDto> FetchUserProfile(string? cookie, string? handle,
        CancellationToken cancellationToken = default);

    Task<DramaListDto> FetchDramaList(string? cookie, string secUid, int count = 20, string cursor = "0",
        CancellationToken cancellationToken = default);

    /// <summary>Một trang các tập của drama <paramref name="dramaId"/> (không cần cookie).</summary>
    Task<DramaEpisodeListDto> FetchDramaEpisodes(string? cookie, string dramaId, int count = 30, string cursor = "0",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tải file video của một video TikTok công khai (<paramref name="videoUrl"/> là link trang video) về
    /// <paramref name="destinationPath"/>. Video thường tải được không cần cookie; tập drama thì TikTok chỉ cấp file cho
    /// phiên đã đăng nhập, nên truyền <paramref name="cookie"/> của tài khoản TikTok (<c>Account.Cookie</c>).
    /// </summary>
    /// <returns>Kích thước file (byte).</returns>
    /// <exception cref="Common.Exceptions.TikTokApiException">Video không công khai, bị chặn, hoặc lớn hơn <paramref name="maxBytes"/>.</exception>
    Task<long> DownloadVideoAsync(string? cookie, string videoUrl, string destinationPath, long maxBytes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tải file video từ các link file TikTok đã có (vd. <c>DramaEpisodeVideoDto.DownloadUrls()</c>) về
    /// <paramref name="destinationPath"/>: thử lần lượt từng link cho tới khi một link tải được. Link file của TikTok chỉ
    /// tải được kèm cookie của phiên đã lấy ra nó.
    /// </summary>
    /// <returns>Kích thước file (byte).</returns>
    /// <exception cref="Common.Exceptions.TikTokApiException">Không link nào tải được, hoặc file lớn hơn <paramref name="maxBytes"/>.</exception>
    Task<long> DownloadVideoFileAsync(string? cookie, IReadOnlyList<string> fileUrls, string destinationPath, long maxBytes,
        CancellationToken cancellationToken = default);
}
