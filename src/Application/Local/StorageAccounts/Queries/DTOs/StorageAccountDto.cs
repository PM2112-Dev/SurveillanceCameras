using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.StorageAccounts.Queries.DTOs;

/// <summary>Thống kê theo tháng (UTC) của tài khoản R2 kèm ước tính chi phí theo bảng giá Cloudflare R2.</summary>
public class StorageAccountMonthlyStatDto
{
    public int Year { get; init; }

    public int Month { get; init; }

    public int VideosUploaded { get; init; }

    public long BytesUploaded { get; init; }

    public int VideosDeleted { get; init; }

    public long BytesDeleted { get; init; }

    public long PutObjectRequests { get; init; }

    public long ListObjectsRequests { get; init; }

    public long OtherClassARequests { get; init; }

    public long TotalClassARequests { get; init; }

    public long GetObjectRequests { get; init; }

    public long HeadObjectRequests { get; init; }

    public long OtherClassBRequests { get; init; }

    public long TotalClassBRequests { get; init; }

    public long DeleteObjectRequests { get; init; }

    public long TotalRequests { get; init; }

    /// <summary>Chi phí lưu trữ ước tính (USD, miễn phí 10 GB đầu, vượt mức $0.015/GB-tháng).</summary>
    public decimal StorageCostUsd { get; init; }

    /// <summary>Chi phí request Class A ước tính (USD, miễn phí 1 triệu request đầu, vượt mức $4.50/triệu).</summary>
    public decimal ClassACostUsd { get; init; }

    /// <summary>Chi phí request Class B ước tính (USD, miễn phí 10 triệu request đầu, vượt mức $0.36/triệu).</summary>
    public decimal ClassBCostUsd { get; init; }

    /// <summary>Tổng chi phí ước tính trong tháng (USD).</summary>
    public decimal TotalEstimatedCostUsd { get; init; }

    public static StorageAccountMonthlyStatDto From(StorageAccountMonthlyStat stat, long currentUsedBytes)
    {
        var classA = stat.TotalClassARequests;
        var classB = stat.TotalClassBRequests;
        var storageCost = StorageAccount.CalculateStorageCostUsd(currentUsedBytes);
        var classACost = StorageAccount.CalculateClassACostUsd(classA);
        var classBCost = StorageAccount.CalculateClassBCostUsd(classB);

        return new StorageAccountMonthlyStatDto
        {
            Year = stat.Year,
            Month = stat.Month,
            VideosUploaded = stat.VideosUploaded,
            BytesUploaded = stat.BytesUploaded,
            VideosDeleted = stat.VideosDeleted,
            BytesDeleted = stat.BytesDeleted,
            PutObjectRequests = stat.PutObjectRequests,
            ListObjectsRequests = stat.ListObjectsRequests,
            OtherClassARequests = stat.OtherClassARequests,
            TotalClassARequests = classA,
            GetObjectRequests = stat.GetObjectRequests,
            HeadObjectRequests = stat.HeadObjectRequests,
            OtherClassBRequests = stat.OtherClassBRequests,
            TotalClassBRequests = classB,
            DeleteObjectRequests = stat.DeleteObjectRequests,
            TotalRequests = stat.TotalRequests,
            StorageCostUsd = storageCost,
            ClassACostUsd = classACost,
            ClassBCostUsd = classBCost,
            TotalEstimatedCostUsd = Math.Round(storageCost + classACost + classBCost, 4)
        };
    }

    public static StorageAccountMonthlyStatDto Empty(int year, int month, long currentUsedBytes)
    {
        var storageCost = StorageAccount.CalculateStorageCostUsd(currentUsedBytes);
        return new StorageAccountMonthlyStatDto
        {
            Year = year,
            Month = month,
            StorageCostUsd = storageCost,
            TotalEstimatedCostUsd = storageCost
        };
    }
}

/// <summary>Tài khoản lưu trữ video Cloudflare R2. Không có Secret Access Key: khoá bí mật chỉ ghi vào, không đọc ra.</summary>
public class StorageAccountDto : BaseDto
{
    public string? CloudflareAccountId { get; init; }

    public string? AccessKeyId { get; init; }

    public string? Bucket { get; init; }

    public string? PublicBaseUrl { get; init; }

    public long QuotaBytes { get; init; }

    public long UsedBytes { get; init; }

    /// <summary>Số video tối đa được tải lên R2 mỗi tháng (0 = không giới hạn).</summary>
    public int MonthlyVideoLimit { get; init; }

    public bool IsEnabled { get; init; }

    public int Priority { get; init; }

    public DateTimeOffset? LastCheckedAt { get; init; }

    public string? LastError { get; init; }

    /// <summary>Số tập phim đang lưu video ở tài khoản này.</summary>
    public int EpisodeCount { get; init; }

    /// <summary>Chi phí lưu trữ ước tính hằng tháng theo dung lượng hiện tại (USD).</summary>
    public decimal StorageCostUsd { get; init; }

    /// <summary>Thống kê của tháng hiện tại (UTC).</summary>
    public StorageAccountMonthlyStatDto CurrentMonth { get; init; } = new();

    /// <summary>Thống kê theo từng tháng (mới nhất trước, tối đa 12 tháng gần nhất).</summary>
    public List<StorageAccountMonthlyStatDto> MonthlyStats { get; init; } = [];
}

public static class StorageAccountQueries
{
    public static async Task<List<StorageAccountDto>> ToDtoListAsync(
        this IQueryable<StorageAccount> accounts,
        IApplicationDbContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var utc = now.UtcDateTime;
        var rows = await accounts
            .Include(x => x.MonthlyStats)
            .Select(x => new
            {
                Account = x,
                EpisodeCount = context.Episodes.Count(e => e.StorageAccountId == x.Id)
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row =>
        {
            var x = row.Account;
            var stats = x.MonthlyStats
                .OrderByDescending(s => s.Year)
                .ThenByDescending(s => s.Month)
                .Take(12)
                .Select(s => StorageAccountMonthlyStatDto.From(s, x.UsedBytes))
                .ToList();

            var current = stats.FirstOrDefault(s => s.Year == utc.Year && s.Month == utc.Month)
                          ?? StorageAccountMonthlyStatDto.Empty(utc.Year, utc.Month, x.UsedBytes);

            if (stats.All(s => s.Year != utc.Year || s.Month != utc.Month))
            {
                stats.Insert(0, current);
            }

            return new StorageAccountDto
            {
                Id = x.Id,
                Title = x.Title,
                CloudflareAccountId = x.CloudflareAccountId,
                AccessKeyId = x.AccessKeyId,
                Bucket = x.Bucket,
                PublicBaseUrl = x.PublicBaseUrl,
                QuotaBytes = x.QuotaBytes,
                UsedBytes = x.UsedBytes,
                MonthlyVideoLimit = x.MonthlyVideoLimit,
                IsEnabled = x.IsEnabled,
                Priority = x.Priority,
                LastCheckedAt = x.LastCheckedAt,
                LastError = x.LastError,
                EpisodeCount = row.EpisodeCount,
                StorageCostUsd = StorageAccount.CalculateStorageCostUsd(x.UsedBytes),
                CurrentMonth = current,
                MonthlyStats = stats
            };
        }).ToList();
    }
}
