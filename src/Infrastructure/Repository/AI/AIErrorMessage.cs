using System.Text.Json;

namespace SurveillanceCameras.Infrastructure.Repository.AI;

internal static class AIErrorMessage
{
    /// <summary>
    /// Thông điệp lỗi trong body: <c>error.message</c> (Gemini, OpenAI, DeepSeek, Anthropic) hoặc <c>error</c> dạng chuỗi (xAI);
    /// không đọc được thì trả nguyên văn (cắt ngắn).
    /// </summary>
    public static string From(string content)
    {
        try
        {
            using var json = JsonDocument.Parse(content);
            if (json.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String) return error.GetString()!;
                if (error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var message)
                    && message.ValueKind == JsonValueKind.String)
                    return message.GetString()!;
            }
        }
        catch (JsonException)
        {
            // Không phải JSON: dùng nguyên văn.
        }

        return Shorten(content);
    }

    private static string Shorten(string text) => text.Length <= 300 ? text : text[..300] + "…";
}
