using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.CrawlStory.Config;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.CrawlStories;

/// <summary>Nguồn web đã nạp sẵn repository và ngữ cảnh crawl (cookie, cấu hình) cho các query crawl.</summary>
public sealed record CrawlSource(ICrawlStoryRepository Repository, CrawlContext Context)
{
    /// <param name="ownerId">
    /// Chủ nguồn web: người dùng hiện tại, hoặc chủ của truyện khi chạy nền (consumer Kafka không có người đăng nhập).
    /// </param>
    public static async Task<CrawlSource> LoadAsync(IApplicationDbContext dbContext, string? ownerId,
        ICrawlStoryRepositoryFactory factory, int webSourceId, CancellationToken cancellationToken)
    {
        var webSource = await dbContext.WebSources
            .AsNoTracking()
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.Id == webSourceId && x.CreatedBy == ownerId, cancellationToken);

        Guard.Against.NotFound(webSourceId, webSource);

        if (string.IsNullOrWhiteSpace(webSource.BaseUrl))
            throw new CrawlException("Nguồn web chưa có Base URL.");

        if (webSource.IsCookie && string.IsNullOrWhiteSpace(webSource.Account?.Cookie))
            throw new CrawlException("Nguồn web cần cookie nhưng tài khoản liên kết chưa có cookie.");

        var config = string.IsNullOrWhiteSpace(webSource.CrawlConfig)
            ? null
            : CrawlConfig.Parse(webSource.CrawlConfig);

        var context = new CrawlContext(webSource.BaseUrl, webSource.IsCookie ? webSource.Account!.Cookie : null, config);

        return new CrawlSource(factory.GetRepository(webSource), context);
    }

    /// <summary>
    /// URL truyện/chương do client gửi lên phải thuộc đúng host của nguồn web — không cho server bị dùng để gọi
    /// tới địa chỉ tuỳ ý (SSRF).
    /// </summary>
    public static bool IsSameHost(string? url, string baseUrl)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
           && Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
           && string.Equals(uri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase);
}
