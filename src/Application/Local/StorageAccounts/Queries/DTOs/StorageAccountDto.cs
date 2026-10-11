using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.StorageAccounts.Queries.DTOs;

/// <summary>Tài khoản lưu trữ video. Không có Secret Access Key: khoá bí mật chỉ ghi vào, không đọc ra.</summary>
public class StorageAccountDto : BaseDto
{
    public string? CloudflareAccountId { get; init; }

    public string? AccessKeyId { get; init; }

    public string? Bucket { get; init; }

    public string? PublicBaseUrl { get; init; }

    public long QuotaBytes { get; init; }

    public long UsedBytes { get; init; }

    public bool IsEnabled { get; init; }

    public int Priority { get; init; }

    public DateTimeOffset? LastCheckedAt { get; init; }

    public string? LastError { get; init; }

    /// <summary>Số tập phim đang lưu video ở tài khoản này.</summary>
    public int EpisodeCount { get; init; }
}

public static class StorageAccountQueries
{
    public static IQueryable<StorageAccountDto> ToDtos(this IQueryable<StorageAccount> accounts, IApplicationDbContext context)
        => accounts.Select(x => new StorageAccountDto
        {
            Id = x.Id,
            Title = x.Title,
            CloudflareAccountId = x.CloudflareAccountId,
            AccessKeyId = x.AccessKeyId,
            Bucket = x.Bucket,
            PublicBaseUrl = x.PublicBaseUrl,
            QuotaBytes = x.QuotaBytes,
            UsedBytes = x.UsedBytes,
            IsEnabled = x.IsEnabled,
            Priority = x.Priority,
            LastCheckedAt = x.LastCheckedAt,
            LastError = x.LastError,
            EpisodeCount = context.Episodes.Count(e => e.StorageAccountId == x.Id)
        });
}
