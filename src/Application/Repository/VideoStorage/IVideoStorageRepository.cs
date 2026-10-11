using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Repository.VideoStorage;

/// <summary>Một bucket Cloudflare R2 kèm khoá truy cập (đã giải mã).</summary>
public record VideoStorageTarget(string CloudflareAccountId, string AccessKeyId, string SecretAccessKey, string Bucket);

public record VideoStorageUsage(long Bytes, int Objects);

/// <summary>Kho lưu file video (Cloudflare R2, giao thức S3).</summary>
/// <remarks>Mọi lỗi kết nối/quyền truy cập ném <see cref="Common.Exceptions.VideoStorageException"/>.</remarks>
public interface IVideoStorageRepository
{
    Task UploadFileAsync(VideoStorageTarget target, string key, string filePath, string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Xoá file; file không còn thì coi như đã xoá.</summary>
    Task DeleteAsync(VideoStorageTarget target, string key, CancellationToken cancellationToken = default);

    /// <summary>Tổng dung lượng và số file đang có trong bucket (duyệt toàn bộ bucket).</summary>
    Task<VideoStorageUsage> GetUsageAsync(VideoStorageTarget target, CancellationToken cancellationToken = default);
}

public static class StorageAccountExtensions
{
    /// <exception cref="Common.Exceptions.VideoStorageException">Tài khoản thiếu cấu hình hoặc không giải mã được khoá bí mật.</exception>
    public static VideoStorageTarget ToTarget(this StorageAccount account, ISecretProtector protector)
    {
        if (string.IsNullOrWhiteSpace(account.CloudflareAccountId) || string.IsNullOrWhiteSpace(account.AccessKeyId)
            || string.IsNullOrWhiteSpace(account.SecretAccessKey) || string.IsNullOrWhiteSpace(account.Bucket))
            throw new Common.Exceptions.VideoStorageException($"Tài khoản lưu trữ \"{account.Title}\" chưa cấu hình đủ.");

        string secret;
        try
        {
            secret = protector.Unprotect(account.SecretAccessKey);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            throw new Common.Exceptions.VideoStorageException(
                $"Không giải mã được Secret Access Key của tài khoản \"{account.Title}\" (khoá mã hoá của hệ thống đã đổi) — nhập lại khoá.", ex);
        }

        return new VideoStorageTarget(account.CloudflareAccountId, account.AccessKeyId, secret, account.Bucket);
    }

    /// <summary>Link công khai của file <paramref name="key"/> trong bucket của tài khoản.</summary>
    public static string PublicUrl(this StorageAccount account, string key)
        => $"{account.PublicBaseUrl!.TrimEnd('/')}/{key}";
}
