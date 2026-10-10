using SurveillanceCameras.Application.Repository.CrawlStory.Config;

namespace SurveillanceCameras.Application.Repository.CrawlStory;

/// <param name="BaseUrl">URL danh sách truyện của nguồn web; mọi request chỉ được đi tới host của URL này.</param>
/// <param name="Cookie">Cookie của tài khoản gắn với nguồn web (khi nguồn cần đăng nhập).</param>
public record CrawlContext(string BaseUrl, string? Cookie, CrawlConfig? Config);
