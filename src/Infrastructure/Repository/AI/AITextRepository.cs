using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Repository.AI;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Infrastructure.Repository.AI;

/// <summary>
/// Sinh văn bản bằng model của người dùng. Claude dùng SDK chính thức của Anthropic; Gemini và nhóm tương thích OpenAI
/// (ChatGPT, DeepSeek, Grok) gọi REST. Mọi lỗi được đổi thành <see cref="AIProviderException"/> để bên gọi chuyển sang key khác.
/// </summary>
public sealed class AITextRepository : IAITextRepository
{
    public const string HttpClientName = "AIText";

    // Đủ cho tên/giới thiệu truyện kể cả khi model có "thinking" (token suy nghĩ tính vào giới hạn này).
    private const int MaxOutputTokens = 16000;

    private static readonly Dictionary<AIProvider, string> ChatCompletionsUrl = new()
    {
        [AIProvider.ChatGpt] = "https://api.openai.com/v1/chat/completions",
        [AIProvider.DeepSeek] = "https://api.deepseek.com/chat/completions",
        [AIProvider.Grok] = "https://api.x.ai/v1/chat/completions",
    };

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;

    public AITextRepository(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientName);
    }

    public async Task<string> GenerateAsync(AITextRequest request, CancellationToken cancellationToken = default)
    {
        var text = request.Provider switch
        {
            AIProvider.Claude => await GenerateClaudeAsync(request, cancellationToken),
            AIProvider.Gemini => await GenerateGeminiAsync(request, cancellationToken),
            _ when ChatCompletionsUrl.TryGetValue(request.Provider, out var url) => await GenerateChatCompletionAsync(request, url, cancellationToken),
            _ => throw new AIProviderException($"Chưa hỗ trợ sinh văn bản với {request.Provider}.")
        };

        return string.IsNullOrWhiteSpace(text)
            ? throw new AIProviderException($"{request.Provider} ({request.Model}) trả về nội dung rỗng.")
            : text.Trim();
    }

    private static async Task<string> GenerateClaudeAsync(AITextRequest request, CancellationToken cancellationToken)
    {
        var client = new AnthropicClient { ApiKey = request.ApiKey };

        Message response;
        try
        {
            response = await client.Messages.Create(new MessageCreateParams
            {
                Model = request.Model,
                MaxTokens = MaxOutputTokens,
                System = request.Instructions,
                Messages = [new() { Role = Role.User, Content = request.Input }]
            }, cancellationToken);
        }
        catch (AnthropicUnauthorizedException ex)
        {
            throw new AIProviderException("Claude từ chối API key (401).", ex);
        }
        catch (AnthropicNotFoundException ex)
        {
            throw new AIProviderException($"Claude không có model '{request.Model}' (404).", ex);
        }
        catch (AnthropicRateLimitException ex)
        {
            throw new AIProviderException("Claude báo vượt giới hạn/quota (429).", ex);
        }
        catch (AnthropicApiException ex)
        {
            throw new AIProviderException($"Claude trả lỗi: {ex.Message}", ex);
        }
        catch (AnthropicIOException ex)
        {
            throw new AIProviderException($"Không kết nối được tới Claude: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not AIProviderException && !cancellationToken.IsCancellationRequested)
        {
            throw new AIProviderException($"Không gọi được Claude ({request.Model}): {ex.Message}", ex);
        }

        // Bị bộ lọc an toàn từ chối: coi như lỗi để bên gọi thử key/model khác.
        if (response.StopReason == "refusal")
            throw new AIProviderException($"Claude ({request.Model}) từ chối xử lý nội dung này.");

        return string.Concat(response.Content.Select(block => block.Value).OfType<TextBlock>().Select(block => block.Text));
    }

    // POST …/models/{model}:generateContent — systemInstruction + contents; trả candidates[0].content.parts[].text.
    private async Task<string> GenerateGeminiAsync(AITextRequest request, CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(request.Model)}:generateContent";
        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                systemInstruction = new { parts = new[] { new { text = request.Instructions } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = request.Input } } } }
            })
        };
        message.Headers.Add("x-goog-api-key", request.ApiKey);

        using var json = await SendAsync(request, message, cancellationToken);

        if (!json.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            var reason = json.RootElement.TryGetProperty("promptFeedback", out var feedback)
                         && feedback.TryGetProperty("blockReason", out var block) ? block.GetString() : null;
            throw new AIProviderException($"Gemini không trả kết quả{(reason is null ? "" : $" (bị chặn: {reason})")}.");
        }

        var candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts))
        {
            var finish = candidate.TryGetProperty("finishReason", out var f) ? f.GetString() : "không rõ";
            throw new AIProviderException($"Gemini không trả nội dung (finishReason: {finish}).");
        }

        return string.Concat(parts.EnumerateArray()
            .Where(part => !(part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True))
            .Select(part => part.TryGetProperty("text", out var text) ? text.GetString() : null));
    }

    // POST {base}/chat/completions (chuẩn OpenAI) — trả choices[0].message.content.
    private async Task<string> GenerateChatCompletionAsync(AITextRequest request, string url, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                model = request.Model,
                messages = new[]
                {
                    new { role = "system", content = request.Instructions },
                    new { role = "user", content = request.Input }
                }
            })
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);

        using var json = await SendAsync(request, message, cancellationToken);

        var choices = json.RootElement.TryGetProperty("choices", out var c) ? c : default;
        if (choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
            throw new AIProviderException($"{request.Provider} không trả kết quả.");

        return choices[0].GetProperty("message").TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String
            ? content.GetString()!
            : throw new AIProviderException($"{request.Provider} trả kết quả không có nội dung.");
    }

    private async Task<JsonDocument> SendAsync(AITextRequest request, HttpRequestMessage message, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Mọi lỗi mạng/hết giờ (HttpRequestException, TaskCanceledException, TimeoutRejectedException của Polly…).
            throw new AIProviderException($"Không gọi được {request.Provider} ({request.Model}): {ex.Message}", ex);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var reason = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "từ chối API key",
                    HttpStatusCode.TooManyRequests => "báo vượt giới hạn/quota",
                    _ => "trả lỗi"
                };
                throw new AIProviderException(
                    $"{request.Provider} ({request.Model}) {reason} ({(int)response.StatusCode}): {AIErrorMessage.From(body)}");
            }

            try
            {
                return JsonDocument.Parse(body);
            }
            catch (JsonException ex)
            {
                throw new AIProviderException($"{request.Provider} trả về dữ liệu không đọc được.", ex);
            }
        }
    }
}
