using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Repository.Media;

namespace SurveillanceCameras.Infrastructure.Repository.Media;

/// <summary>Lưu media vào thư mục trên máy chủ (<see cref="MediaOptions.RootDirectory"/>), phục vụ qua <c>/media</c>.</summary>
public sealed partial class LocalMediaRepository : IMediaRepository
{
    public const string HttpClientName = "Media";

    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/jpg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
        ["image/avif"] = ".avif",
        ["image/heic"] = ".heic"
    };

    private readonly HttpClient _httpClient;
    private readonly MediaOptions _options;

    public LocalMediaRepository(IHttpClientFactory httpClientFactory, IOptions<MediaOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientName);
        _options = options.Value;
    }

    public string Root => Path.GetFullPath(_options.RootDirectory);

    public async Task<string> SaveImageFromUrlAsync(string sourceUrl, string folder, string name,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new MediaException($"Link ảnh không hợp lệ (chỉ nhận https): {sourceUrl}");
        if (!SafeSegment().IsMatch(name) || folder.Split('/').Any(part => !SafeSegment().IsMatch(part)))
            throw new MediaException($"Tên file/thư mục media không hợp lệ: {folder}/{name}");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        // Mọi lỗi mạng/hết giờ, kể cả TimeoutRejectedException của lớp resilience (Polly).
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MediaException($"Không tải được ảnh: {ex.Message}", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new MediaException($"Tải ảnh lỗi HTTP {(int)response.StatusCode}.");

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!Extensions.TryGetValue(contentType, out var extension))
                throw new MediaException($"Link không phải ảnh (Content-Type: {contentType}).");
            if (response.Content.Headers.ContentLength > _options.MaxImageBytes)
                throw new MediaException("Ảnh quá lớn.");

            var directory = Path.Combine(Root, folder);
            Directory.CreateDirectory(directory);

            // Ghi ra file tạm rồi đổi tên: lỗi giữa chừng không để lại ảnh hỏng thay cho ảnh cũ.
            var fileName = name + extension;
            var target = Path.Combine(directory, fileName);
            var temp = target + ".tmp";
            try
            {
                await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
                await using (var output = File.Create(temp))
                {
                    var buffer = new byte[81920];
                    long total = 0;
                    int read;
                    while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        total += read;
                        if (total > _options.MaxImageBytes) throw new MediaException("Ảnh quá lớn.");
                        await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    }
                }

                // Xoá bản cũ khác đuôi (vd. trước là .png, nay là .jpg).
                foreach (var old in Directory.EnumerateFiles(directory, name + ".*").Where(f => f != temp && f != target))
                    File.Delete(old);
                File.Move(temp, target, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new MediaException($"Không ghi được ảnh vào kho: {ex.Message}", ex);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }

            return $"{MediaOptions.RequestPath}/{folder}/{fileName}";
        }
    }

    public bool IsStored(string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith(MediaOptions.RequestPath + "/", StringComparison.Ordinal))
            return false;

        var relative = url[(MediaOptions.RequestPath.Length + 1)..];
        var full = Path.GetFullPath(Path.Combine(Root, relative));
        return full.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(full);
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,100}$")]
    private static partial Regex SafeSegment();
}
