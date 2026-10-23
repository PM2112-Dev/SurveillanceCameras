namespace SurveillanceCameras.Domain.Entities;

/// <summary>
/// Thống kê theo tháng (UTC) của tài khoản lưu trữ Cloudflare R2: số lượng và dung lượng video tải lên/xoá, cùng số
/// request theo từng loại (Class A, Class B, Delete) để theo dõi hạn mức miễn phí và ước tính chi phí hằng tháng.
/// </summary>
public class StorageAccountMonthlyStat : BaseAuditableEntity
{
    public int StorageAccountId { get; set; }

    /// <summary>Năm UTC (vd. 2026).</summary>
    public int Year { get; set; }

    /// <summary>Tháng UTC (1–12).</summary>
    public int Month { get; set; }

    /// <summary>Số video được tải lên R2 trong tháng.</summary>
    public int VideosUploaded { get; set; }

    /// <summary>Tổng dung lượng các video tải lên trong tháng (byte).</summary>
    public long BytesUploaded { get; set; }

    /// <summary>Số video bị xoá khỏi R2 trong tháng.</summary>
    public int VideosDeleted { get; set; }

    /// <summary>Tổng dung lượng các video bị xoá trong tháng (byte).</summary>
    public long BytesDeleted { get; set; }

    // ── Request Class A ($4.50 / 1 triệu sau 1 triệu request miễn phí) ─────────

    /// <summary>Số request tải file lên (<c>PutObject</c>).</summary>
    public long PutObjectRequests { get; set; }

    /// <summary>Số request duyệt danh sách file trong bucket (<c>ListObjectsV2</c>).</summary>
    public long ListObjectsRequests { get; set; }

    /// <summary>Số request Class A khác (CopyObject, MultipartUpload, hoặc số đối soát thêm từ Cloudflare).</summary>
    public long OtherClassARequests { get; set; }

    // ── Request Class B ($0.36 / 1 triệu sau 10 triệu request miễn phí) ────────

    /// <summary>Số request đọc/phát file (<c>GetObject</c>).</summary>
    public long GetObjectRequests { get; set; }

    /// <summary>Số request kiểm tra thông tin file (<c>HeadObject</c>).</summary>
    public long HeadObjectRequests { get; set; }

    /// <summary>Số request Class B khác (hoặc số đối soát thêm từ Cloudflare).</summary>
    public long OtherClassBRequests { get; set; }

    // ── Request miễn phí ($0) ──────────────────────────────────────────────────

    /// <summary>Số request xoá file (<c>DeleteObject</c> — Cloudflare R2 miễn phí).</summary>
    public long DeleteObjectRequests { get; set; }

    public StorageAccount? StorageAccount { get; set; }

    public long TotalClassARequests => PutObjectRequests + ListObjectsRequests + OtherClassARequests;

    public long TotalClassBRequests => GetObjectRequests + HeadObjectRequests + OtherClassBRequests;

    public long TotalRequests => TotalClassARequests + TotalClassBRequests + DeleteObjectRequests;
}
