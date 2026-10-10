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
}
