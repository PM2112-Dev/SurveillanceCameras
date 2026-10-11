using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Infrastructure.Repository.AccountSession;

namespace SurveillanceCameras.Infrastructure.Repository.TikTok;

public sealed partial class TikTokRepository
{
    public async Task<long> DownloadVideoAsync(string? cookie, string videoUrl, string destinationPath, long maxBytes,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(videoUrl, UriKind.Absolute, out var pageUri) || pageUri.Scheme != Uri.UriSchemeHttps
            || !pageUri.Host.Equals(new Uri(_options.BaseUrl).Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new TikTokApiException($"Link video TikTok không hợp lệ: {videoUrl}");
        }

        // Link file video chỉ tải được kèm cookie phiên (tt_chain_token) mà TikTok cấp khi mở trang video, nên mỗi lần tải
        // dùng một phiên cookie riêng: mở trang lấy link + cookie, rồi tải file bằng chính phiên đó. Cookie của tài khoản
        // (nếu có) được nạp sẵn vào phiên để TikTok trả trang của người đã đăng nhập.
        using var handler = new SocketsHttpHandler
        {
            CookieContainer = SessionCookies(cookie, pageUri),
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.All
        };
        using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", _options.Language);
        client.DefaultRequestHeaders.Referrer = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.VideoDownloadTimeoutSeconds));

        try
        {
            string html;
            using (var page = await client.GetAsync(pageUri, timeout.Token))
            {
                if (page.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
                    throw new TikTokBlockedException($"TikTok chặn máy chủ (HTTP {(int)page.StatusCode}) khi mở trang video.");
                if (!page.IsSuccessStatusCode)
                    throw new TikTokApiException($"TikTok trả về HTTP {(int)page.StatusCode} khi mở trang video.");
                html = await page.Content.ReadAsStringAsync(timeout.Token);
            }

            var playUrl = ParseVideoPlayUrl(html);

            using var response = await client.GetAsync(playUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new TikTokApiException($"TikTok trả về HTTP {(int)response.StatusCode} khi tải file video.");

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                throw new TikTokApiException($"Link tải của TikTok không trả về video (Content-Type: {contentType}).");
            if (response.Content.Headers.ContentLength > maxBytes)
                throw new TikTokApiException("File video lớn hơn mức cho phép.");

            long total = 0;
            await using (var input = await response.Content.ReadAsStreamAsync(timeout.Token))
            await using (var output = File.Create(destinationPath))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await input.ReadAsync(buffer, timeout.Token)) > 0)
                {
                    total += read;
                    if (total > maxBytes) throw new TikTokApiException("File video lớn hơn mức cho phép.");
                    await output.WriteAsync(buffer.AsMemory(0, read), timeout.Token);
                }
            }

            if (total == 0) throw new TikTokApiException("TikTok trả về file video rỗng.");

            return total;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TikTokApiException($"Quá thời gian ({_options.VideoDownloadTimeoutSeconds}s) tải video từ TikTok.");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            throw new TikTokApiException("Không tải được video từ TikTok: " + ex.Message, ex);
        }
    }

    /// <summary>Phiên cookie mới, nạp sẵn cookie đã lưu của tài khoản cho tên miền TikTok và các tên miền con.</summary>
    public static CookieContainer SessionCookies(string? storedCookie, Uri pageUri)
    {
        var container = new CookieContainer();
        var host = pageUri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? pageUri.Host[4..] : pageUri.Host;

        foreach (var part in StoredCookieHeader.Build(storedCookie)
                     .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0) continue;

            try
            {
                container.Add(new Cookie(part[..eq], part[(eq + 1)..], "/", "." + host));
            }
            catch (CookieException)
            {
                // Cookie có ký tự .NET không nhận (dấu phẩy…): bỏ qua, các cookie đăng nhập không thuộc loại này.
            }
        }

        return container;
    }

    /// <summary>Link file video trong dữ liệu nhúng của trang video TikTok.</summary>
    /// <exception cref="TikTokBlockedException">Trang không có dữ liệu: TikTok trả trang thử thách WAF thay cho trang video.</exception>
    /// <exception cref="TikTokVideoUnavailableException">Video không xem công khai được, hoặc TikTok không cấp link file.</exception>
    public static Uri ParseVideoPlayUrl(string html)
    {
        var match = RehydrationData().Match(html);
        if (!match.Success)
            throw new TikTokBlockedException("Trang video TikTok không có dữ liệu (TikTok đang chặn máy chủ bằng trang thử thách, hoặc đã đổi cấu trúc).");

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(match.Groups[1].Value);
        }
        catch (JsonException ex)
        {
            throw new TikTokApiException("Dữ liệu trang video TikTok không phải JSON hợp lệ.", ex);
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("__DEFAULT_SCOPE__", out var scope)
                || !scope.TryGetProperty("webapp.video-detail", out var detail))
            {
                throw new TikTokBlockedException("Trang TikTok không có mục video-detail (TikTok đang chặn máy chủ, hoặc đã đổi cấu trúc).");
            }

            var statusCode = detail.TryGetProperty("statusCode", out var sc) && sc.TryGetInt32(out var code) ? code : 0;
            if (statusCode != 0)
            {
                throw new TikTokVideoUnavailableException(
                    $"TikTok không cho xem video này (statusCode={statusCode}: {ReadString(detail, "statusMsg")}) — video riêng tư, đã xoá hoặc bị giới hạn.");
            }

            if (!detail.TryGetProperty("itemInfo", out var info) || !info.TryGetProperty("itemStruct", out var item)
                || !item.TryGetProperty("video", out var video))
            {
                throw new TikTokApiException("Trang video TikTok không có thông tin video.");
            }

            var url = ReadString(video, "playAddr");
            // Video chỉ có các bản theo bitrate (không có playAddr chung): lấy bản đầu tiên, là bản chất lượng cao nhất.
            if (string.IsNullOrEmpty(url) && video.TryGetProperty("bitrateInfo", out var bitrates)
                                          && bitrates.ValueKind == JsonValueKind.Array)
            {
                url = bitrates.EnumerateArray()
                    .Select(x => x.TryGetProperty("PlayAddr", out var play) && play.TryGetProperty("UrlList", out var list)
                                                                           && list.ValueKind == JsonValueKind.Array
                        ? list.EnumerateArray().Select(u => u.GetString()).FirstOrDefault(u => !string.IsNullOrEmpty(u))
                        : null)
                    .FirstOrDefault(x => x is not null);
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new TikTokVideoUnavailableException("TikTok không cấp link tải cho video này. Tập drama cần cookie của tài khoản TikTok còn đăng nhập (mở lại phiên ở mục Tài khoản nếu đã hết hạn); tập bị khoá thì tài khoản phải xem được.");

            return uri;
        }
    }

    [GeneratedRegex("<script[^>]*id=\"__UNIVERSAL_DATA_FOR_REHYDRATION__\"[^>]*>(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex RehydrationData();
}
