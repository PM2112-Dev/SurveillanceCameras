using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Service.TikTokApi;
using SurveillanceCameras.Application.TikTokApi;

namespace SurveillanceCameras.Infrastructure.Service.TikTokApi;

public sealed class TikTokApiService : ITikTokApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly TikTokApiOptions _options;

    public TikTokApiService(HttpClient httpClient, IOptions<TikTokApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<DramaListDto> GetDramaListAsync(
        string? cookie, string secUid, int count = 20, string cursor = "0", CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(secUid);

        var query = new (string Key, string Value)[]
        {
            ("aid", "1180"),
            ("app_language", _options.Language),
            ("app_name", "tiktok_web"),
            ("channel", "tiktok_web"),
            ("count", count.ToString()),
            ("coverFormat", "2"),
            ("cursor", cursor),
            ("device_platform", "web_pc"),
            ("from_page", "user"),
            ("language", _options.Language),
            ("priority_region", _options.Region),
            ("region", _options.Region),
            ("secUid", secUid),
        };
        var url = "api/drama/user/drama_list/?" + string.Join('&',
            query.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        request.Headers.Referrer = new Uri(_httpClient.BaseAddress!, "/");

        var cookieHeader = TikTokCookieHeader.Build(cookie);
        if (cookieHeader.Length > 0)
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new TikTokApiException("Không kết nối được tới TikTok.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new TikTokApiException($"TikTok trả về HTTP {(int)response.StatusCode} ({response.StatusCode}).");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(body))
            {
                throw new TikTokApiException("TikTok trả về phản hồi rỗng (request bị chặn hoặc cookie đã hết hạn).");
            }

            DramaListDto? result;
            try
            {
                result = JsonSerializer.Deserialize<DramaListDto>(body, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new TikTokApiException("Phản hồi của TikTok không đúng định dạng JSON mong đợi.", ex);
            }

            if (result is null)
            {
                throw new TikTokApiException("Phản hồi của TikTok rỗng.");
            }

            if (result.StatusCode != 0)
            {
                throw new TikTokApiException(
                    $"TikTok báo lỗi (statusCode={result.StatusCode}): {result.StatusMessage}".TrimEnd(' ', ':'));
            }

            return result;
        }
    }
}
