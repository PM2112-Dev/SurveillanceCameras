using System.Text.Json;

namespace SurveillanceCameras.Infrastructure.Repository.AI;

public static class AIErrorMessage
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

    /// <summary>
    /// Thời gian nền tảng báo phải chờ, vd. Gemini: <c>Please retry in 8h28m20.59s</c> hoặc <c>"retryDelay": "30s"</c>;
    /// không có thì <c>null</c>.
    /// </summary>
    public static TimeSpan? RetryDelay(string content)
    {
        var match = System.Text.RegularExpressions.Regex.Match(content,
            @"(?:retry in|retryDelay""\s*:\s*"")\s*(?:(?<h>\d+)h)?(?:(?<m>\d+)m(?!s))?(?:(?<s>\d+(?:\.\d+)?)s)?",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success) return null;

        double Part(string name) => match.Groups[name].Success
            ? double.Parse(match.Groups[name].Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
        var total = TimeSpan.FromHours(Part("h")) + TimeSpan.FromMinutes(Part("m")) + TimeSpan.FromSeconds(Part("s"));
        return total > TimeSpan.Zero ? total : null;
    }

    private static string Shorten(string text) => text.Length <= 300 ? text : text[..300] + "…";
}
