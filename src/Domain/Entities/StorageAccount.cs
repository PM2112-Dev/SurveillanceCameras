namespace SurveillanceCameras.Domain.Entities;

/// <summary>
/// Tài khoản Cloudflare R2 duy nhất (một bucket) dùng để lưu file video của tập phim, kèm theo dõi hạn mức dung lượng,
/// giới hạn số video tải lên mỗi tháng và thống kê số request theo loại (Class A, Class B, Delete) để tính chi phí.
/// </summary>
public class StorageAccount : BaseAuditableEntity
{
    /// <summary>
    /// Số byte của 1 GB theo cách Cloudflare tính dung lượng R2 (hệ thập phân: 1 GB = 1 tỷ byte, không phải 1024³).
    /// Mọi quy đổi hạn mức/dung lượng phải dùng hằng số này, nếu không "9,5 GB" nhập vào sẽ thành 10,2 GB trên Cloudflare.
    /// </summary>
    public const long BytesPerGb = 1_000_000_000;

    public const long BytesPerMb = 1_000_000;

    /// <summary>10 GB: mức miễn phí lưu trữ hằng tháng của Cloudflare R2.</summary>
    public const long DefaultQuotaBytes = 10 * BytesPerGb;

    /// <summary>10 GB/tháng miễn phí lưu trữ trên Cloudflare R2.</summary>
    public const long FreeStorageBytes = 10 * BytesPerGb;

    /// <summary>1.000.000 request Class A miễn phí mỗi tháng trên Cloudflare R2.</summary>
    public const long FreeClassARequests = 1_000_000;

    /// <summary>10.000.000 request Class B miễn phí mỗi tháng trên Cloudflare R2.</summary>
    public const long FreeClassBRequests = 10_000_000;

    /// <summary>Đơn giá lưu trữ vượt mức miễn phí: $0.015 / GB-tháng.</summary>
    public const decimal StoragePricePerGbMonth = 0.015m;

    /// <summary>Đơn giá request Class A vượt mức miễn phí: $4.50 / 1 triệu request.</summary>
    public const decimal ClassAPricePerMillion = 4.50m;

    /// <summary>Đơn giá request Class B vượt mức miễn phí: $0.36 / 1 triệu request.</summary>
    public const decimal ClassBPricePerMillion = 0.36m;

    /// <summary>Account ID của Cloudflare (dựng ra endpoint S3 <c>https://{id}.r2.cloudflarestorage.com</c>).</summary>
    public string? CloudflareAccountId { get; set; }

    public string? AccessKeyId { get; set; }

    /// <summary>Secret Access Key đã mã hoá (không bao giờ trả ra API).</summary>
    public string? SecretAccessKey { get; set; }

    public string? Bucket { get; set; }

    /// <summary>Địa chỉ công khai của bucket (<c>https://pub-….r2.dev</c> hoặc domain riêng) — ghép với khoá file thành link phát.</summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>Dung lượng tối đa được dùng ở tài khoản này (byte).</summary>
    public long QuotaBytes { get; set; } = DefaultQuotaBytes;

    /// <summary>Dung lượng hệ thống đã lưu vào tài khoản (byte) — cộng dồn khi tải lên, đối soát lại từ bucket khi kiểm tra.</summary>
    public long UsedBytes { get; set; }

    /// <summary>Số video tối đa được tải lên R2 mỗi tháng (0 = không giới hạn).</summary>
    public int MonthlyVideoLimit { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>Giữ lại để tương thích lược đồ dữ liệu cũ (mặc định 0).</summary>
    public int Priority { get; set; }

    public DateTimeOffset? LastCheckedAt { get; set; }

    /// <summary>Lỗi của lần kiểm tra kết nối/tải lên gần nhất; <c>null</c> khi lần gần nhất thành công.</summary>
    public string? LastError { get; set; }

    public IList<StorageAccountMonthlyStat> MonthlyStats { get; private set; } = new List<StorageAccountMonthlyStat>();

    public long FreeBytes => Math.Max(0, QuotaBytes - UsedBytes);

    /// <summary>Chi phí lưu trữ ước tính trong tháng (USD) theo dung lượng đang dùng (miễn phí 10 GB đầu).</summary>
    public static decimal CalculateStorageCostUsd(long usedBytes)
    {
        var billableBytes = Math.Max(0, usedBytes - FreeStorageBytes);
        return Math.Round((billableBytes / (decimal)BytesPerGb) * StoragePricePerGbMonth, 4);
    }

    /// <summary>Chi phí request Class A ước tính trong tháng (USD, miễn phí 1 triệu request đầu).</summary>
    public static decimal CalculateClassACostUsd(long classARequests)
    {
        var billable = Math.Max(0, classARequests - FreeClassARequests);
        return Math.Round((billable / 1_000_000m) * ClassAPricePerMillion, 4);
    }

    /// <summary>Chi phí request Class B ước tính trong tháng (USD, miễn phí 10 triệu request đầu).</summary>
    public static decimal CalculateClassBCostUsd(long classBRequests)
    {
        var billable = Math.Max(0, classBRequests - FreeClassBRequests);
        return Math.Round((billable / 1_000_000m) * ClassBPricePerMillion, 4);
    }
}
