using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.StorageAccounts;

/// <summary>
/// Ghi nhận số lượng video tải lên/xoá và số request theo loại (Class A, Class B, Delete) cho tài khoản lưu trữ R2 theo
/// từng tháng (UTC).
/// </summary>
public static class StorageAccountUsageTracker
{
    /// <summary>Số key tối đa mỗi trang <c>ListObjectsV2</c> của S3/R2.</summary>
    public const int ObjectsPerListRequest = 1000;

    /// <summary>Số request <c>ListObjectsV2</c> (Class A) đã thực hiện để đọc hết <paramref name="objectCount"/> file.</summary>
    public static int CountListRequests(int objectCount)
        => Math.Max(1, (objectCount + ObjectsPerListRequest - 1) / ObjectsPerListRequest);

    /// <summary>
    /// Lấy (hoặc khởi tạo trong bộ nhớ nếu chưa có) bản ghi thống kê tháng UTC tương ứng với <paramref name="now"/> của
    /// tài khoản lưu trữ <paramref name="storageAccountId"/>.
    /// </summary>
    public static async Task<StorageAccountMonthlyStat> GetOrCreateMonthlyStatAsync(
        this IApplicationDbContext context,
        int storageAccountId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var utc = now.UtcDateTime;
        return await context.GetOrCreateMonthlyStatAsync(storageAccountId, utc.Year, utc.Month, cancellationToken);
    }

    /// <summary>
    /// Lấy (hoặc khởi tạo trong bộ nhớ nếu chưa có) bản ghi thống kê tháng (<paramref name="year"/>, <paramref name="month"/>)
    /// của tài khoản lưu trữ <paramref name="storageAccountId"/>.
    /// </summary>
    public static async Task<StorageAccountMonthlyStat> GetOrCreateMonthlyStatAsync(
        this IApplicationDbContext context,
        int storageAccountId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var tracked = context.ChangeTracker.Entries<StorageAccountMonthlyStat>()
            .FirstOrDefault(e => e.State != EntityState.Deleted
                                 && e.Entity.StorageAccountId == storageAccountId
                                 && e.Entity.Year == year
                                 && e.Entity.Month == month)
            ?.Entity;

        if (tracked is not null) return tracked;

        var existing = await context.StorageAccountMonthlyStats
            .FirstOrDefaultAsync(x => x.StorageAccountId == storageAccountId && x.Year == year && x.Month == month,
                cancellationToken);

        if (existing is not null) return existing;

        var created = new StorageAccountMonthlyStat
        {
            StorageAccountId = storageAccountId,
            Year = year,
            Month = month
        };
        context.StorageAccountMonthlyStats.Add(created);
        return created;
    }
}
