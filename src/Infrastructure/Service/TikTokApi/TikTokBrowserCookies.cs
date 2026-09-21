using System.Text.Json;
using Microsoft.Playwright;

namespace SurveillanceCameras.Infrastructure.Service.TikTokApi;

/// <summary>Chuyển <c>Account.Cookie</c> thành cookie để nạp vào một context của Playwright.</summary>
public static class TikTokBrowserCookies
{
    private sealed record StoredCookie(
        string? Name, string? Value, string? Domain, string? Path,
        double? Expires, bool? HttpOnly, bool? Secure, string? SameSite);

    public static List<Cookie> ToPlaywright(string? stored, string baseUrl, DateTimeOffset? now = null)
    {
        var result = new List<Cookie>();
        if (string.IsNullOrWhiteSpace(stored)) return result;

        var text = stored.Trim();
        var nowSeconds = (now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();

        if (text.StartsWith('['))
        {
            var cookies = JsonSerializer.Deserialize<List<StoredCookie>>(
                text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            foreach (var c in cookies)
            {
                if (string.IsNullOrEmpty(c.Name) || c.Value is null || string.IsNullOrEmpty(c.Domain)) continue;
                if (c.Expires is > 0 && c.Expires < nowSeconds) continue;

                result.Add(new Cookie
                {
                    Name = c.Name,
                    Value = c.Value,
                    Domain = c.Domain,
                    Path = string.IsNullOrEmpty(c.Path) ? "/" : c.Path,
                    Expires = c.Expires is > 0 ? (float)c.Expires : null,
                    HttpOnly = c.HttpOnly,
                    Secure = c.Secure,
                    SameSite = Enum.TryParse<SameSiteAttribute>(c.SameSite, true, out var sameSite) ? sameSite : null,
                });
            }

            return result;
        }

        // Chuỗi dán tay "a=b; c=d": không có domain nên gắn theo URL gốc.
        var raw = text.StartsWith("cookie:", StringComparison.OrdinalIgnoreCase) ? text[7..] : text;
        foreach (var part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0) continue;

            result.Add(new Cookie { Name = part[..eq], Value = part[(eq + 1)..], Url = baseUrl });
        }

        return result;
    }
}
