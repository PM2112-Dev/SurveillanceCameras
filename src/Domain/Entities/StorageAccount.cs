namespace SurveillanceCameras.Domain.Entities;

/// <summary>
/// Tài khoản Cloudflare R2 (một bucket) dùng để lưu file video của tập phim. Có nhiều tài khoản thì video được lưu vào
/// tài khoản đang bật, ưu tiên cao nhất còn đủ chỗ; đầy thì tự chuyển sang tài khoản kế tiếp.
/// </summary>
public class StorageAccount : BaseAuditableEntity
{
    /// <summary>
    /// Số byte của 1 GB theo cách Cloudflare tính dung lượng R2 (hệ thập phân: 1 GB = 1 tỷ byte, không phải 1024³).
    /// Mọi quy đổi hạn mức/dung lượng phải dùng hằng số này, nếu không "9,5 GB" nhập vào sẽ thành 10,2 GB trên Cloudflare.
    /// </summary>
    public const long BytesPerGb = 1_000_000_000;

    public const long BytesPerMb = 1_000_000;

    /// <summary>10 GB: mức miễn phí của Cloudflare R2.</summary>
    public const long DefaultQuotaBytes = 10 * BytesPerGb;

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

    public bool IsEnabled { get; set; } = true;

    /// <summary>Số nhỏ được dùng trước.</summary>
    public int Priority { get; set; }

    public DateTimeOffset? LastCheckedAt { get; set; }

    /// <summary>Lỗi của lần kiểm tra kết nối/tải lên gần nhất; <c>null</c> khi lần gần nhất thành công.</summary>
    public string? LastError { get; set; }

    public long FreeBytes => Math.Max(0, QuotaBytes - UsedBytes);
}
