using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Repository.AI;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Infrastructure.Repository.AI;

/// <summary>
/// Gọi endpoint "list models" của từng nền tảng. Chỉ đọc danh sách nên dùng HTTP thẳng thay vì SDK riêng của từng hãng.
/// API key luôn gửi trong header, không đặt trên URL (URL hay bị ghi log).
/// </summary>
public sealed class AIModelRepository : IAIModelRepository
{
    public const string HttpClientName = "AIProviders";

    private const string AnthropicVersion = "2023-06-01";
    private const int MaxPages = 20;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // Endpoint theo chuẩn OpenAI (GET {base}/models, Bearer key, trả { data: [{ id }] }).
    private static readonly Dictionary<AIProvider, string> OpenAiCompatibleModelsUrl = new()
    {
        [AIProvider.ChatGpt] = "https://api.openai.com/v1/models",
        [AIProvider.DeepSeek] = "https://api.deepseek.com/models",
        [AIProvider.Grok] = "https://api.x.ai/v1/models",
    };

    private readonly HttpClient _httpClient;

    public AIModelRepository(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientName);
    }

    public Task<List<AIModelDto>> ListModelsAsync(AIProvider provider, string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new AIProviderException("Chưa có API key.");

        return provider switch
        {
            AIProvider.Gemini => ListGeminiAsync(apiKey, cancellationToken),
            AIProvider.Claude => ListClaudeAsync(apiKey, cancellationToken),
            _ when OpenAiCompatibleModelsUrl.TryGetValue(provider, out var url) => ListOpenAiCompatibleAsync(provider, url, apiKey, cancellationToken),
            _ => throw new AIProviderException($"Chưa hỗ trợ lấy danh sách model của {provider}.")
        };
    }

    // GET https://generativelanguage.googleapis.com/v1beta/models → { models: [{ name: "models/…", displayName,
    // supportedGenerationMethods }], nextPageToken }. Chỉ giữ model sinh nội dung (bỏ embedding…).
    private async Task<List<AIModelDto>> ListGeminiAsync(string apiKey, CancellationToken cancellationToken)
    {
        var models = new List<AIModelDto>();
        string? pageToken = null;

        for (var page = 0; page < MaxPages; page++)
        {
            var url = "https://generativelanguage.googleapis.com/v1beta/models?pageSize=1000"
                      + (pageToken is null ? "" : "&pageToken=" + Uri.EscapeDataString(pageToken));
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-goog-api-key", apiKey);

            var body = await SendAsync<GeminiModelList>(AIProvider.Gemini, request, cancellationToken);
            models.AddRange((body.Models ?? [])
                .Where(m => m.Name is not null && (m.SupportedGenerationMethods?.Contains("generateContent") ?? true))
                .Select(m => new AIModelDto(m.Name!.StartsWith("models/") ? m.Name[7..] : m.Name, m.DisplayName)));

            pageToken = body.NextPageToken;
            if (string.IsNullOrEmpty(pageToken)) break;
        }

        return models;
    }

    // GET https://api.anthropic.com/v1/models (x-api-key, anthropic-version) → { data: [{ id, display_name }],
    // has_more, last_id }; trang sau truyền after_id.
    private async Task<List<AIModelDto>> ListClaudeAsync(string apiKey, CancellationToken cancellationToken)
    {
        var models = new List<AIModelDto>();
        string? afterId = null;

        for (var page = 0; page < MaxPages; page++)
        {
            var url = "https://api.anthropic.com/v1/models?limit=1000"
                      + (afterId is null ? "" : "&after_id=" + Uri.EscapeDataString(afterId));
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", AnthropicVersion);

            var body = await SendAsync<ClaudeModelList>(AIProvider.Claude, request, cancellationToken);
            models.AddRange((body.Data ?? []).Where(m => m.Id is not null).Select(m => new AIModelDto(m.Id!, m.Display_Name)));

            if (body.Has_More != true || string.IsNullOrEmpty(body.Last_Id)) break;
            afterId = body.Last_Id;
        }

        return models;
    }

    private async Task<List<AIModelDto>> ListOpenAiCompatibleAsync(AIProvider provider, string url, string apiKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var body = await SendAsync<OpenAiModelList>(provider, request, cancellationToken);
        return (body.Data ?? []).Where(m => m.Id is not null).Select(m => new AIModelDto(m.Id!, null)).ToList();
    }

    private async Task<T> SendAsync<T>(AIProvider provider, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AIProviderException($"Không kết nối được tới {provider}: {ex.Message}", ex);
        }

        using (response)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new AIProviderException($"{provider} từ chối API key ({(int)response.StatusCode}) — key sai, đã bị thu hồi hoặc không có quyền.");

            // Gemini và Grok báo key sai bằng 400 thay vì 401 — lấy thông điệp lỗi của nền tảng cho dễ đọc.
            if (!response.IsSuccessStatusCode)
                throw new AIProviderException($"{provider} trả lỗi {(int)response.StatusCode}: {AIErrorMessage.From(content)}");

            try
            {
                return JsonSerializer.Deserialize<T>(content, JsonOptions)
                       ?? throw new AIProviderException($"{provider} trả về phản hồi rỗng.");
            }
            catch (JsonException ex)
            {
                throw new AIProviderException($"{provider} trả về dữ liệu không đọc được.", ex);
            }
        }
    }


    private sealed record GeminiModelList(List<GeminiModel>? Models, string? NextPageToken);

    private sealed record GeminiModel(string? Name, string? DisplayName, List<string>? SupportedGenerationMethods);

    // Tên thuộc tính khớp snake_case của Anthropic (JSON không phân biệt hoa thường nhưng có dấu "_").
    private sealed record ClaudeModelList(List<ClaudeModel>? Data, bool? Has_More, string? Last_Id);

    private sealed record ClaudeModel(string? Id, string? Display_Name);

    private sealed record OpenAiModelList(List<OpenAiModel>? Data);

    private sealed record OpenAiModel(string? Id);
}
