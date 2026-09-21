using System.Text;
using System.Text.Json;

namespace SurveillanceCameras.Infrastructure.Service.TikTokApi;

/// <summary>
/// Chuyển giá trị <c>Account.Cookie</c> thành header <c>Cookie</c>. Nhận hai định dạng:
/// JSON mảng cookie (do phiên Chrome trên server lưu) hoặc chuỗi <c>name=value; name2=value2</c> dán tay.
/// </summary>
public static class TikTokCookieHeader
{
    private sealed record StoredCookie(string? Name, string? Value, double? Expires);

    public static string Build(string? stored, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(stored)) return string.Empty;

        var text = stored.Trim();
        if (!text.StartsWith('['))
        {
            return text.StartsWith("cookie:", StringComparison.OrdinalIgnoreCase) ? text[7..].Trim() : text;
        }

        var cookies = JsonSerializer.Deserialize<List<StoredCookie>>(
            text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        var nowSeconds = (now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        var header = new StringBuilder();

        foreach (var cookie in cookies)
        {
            if (string.IsNullOrEmpty(cookie.Name) || cookie.Value is null) continue;
            // Cookie phiên có Expires = -1; cookie có hạn đã quá hạn thì bỏ.
            if (cookie.Expires is > 0 && cookie.Expires < nowSeconds) continue;
            // Chặn ký tự điều khiển để giá trị cookie không chèn thêm header.
            if (cookie.Name.Any(char.IsControl) || cookie.Value.Any(char.IsControl)) continue;

            if (header.Length > 0) header.Append("; ");
            header.Append(cookie.Name).Append('=').Append(cookie.Value);
        }

        return header.ToString();
    }
}
