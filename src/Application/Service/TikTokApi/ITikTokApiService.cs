namespace SurveillanceCameras.Application.TikTokApi;

/// <summary>Gọi API web của TikTok bằng cookie của một tài khoản.</summary>
public interface ITikTokApiService
{
    /// <summary>
    /// Lấy danh sách drama của một kênh.
    /// </summary>
    /// <param name="cookie">Giá trị <c>Account.Cookie</c> (JSON do phiên Chrome lưu, hoặc chuỗi <c>a=b; c=d</c>).</param>
    /// <param name="secUid">secUid của kênh TikTok cần lấy.</param>
    /// <param name="count">Số item mỗi trang.</param>
    /// <param name="cursor">Con trỏ trang ("0" cho trang đầu; dùng <see cref="DramaListDto.Cursor"/> cho trang sau).</param>
    Task<DramaListDto> GetDramaListAsync(
        string? cookie, string secUid, int count = 20, string cursor = "0", CancellationToken cancellationToken = default);
}
