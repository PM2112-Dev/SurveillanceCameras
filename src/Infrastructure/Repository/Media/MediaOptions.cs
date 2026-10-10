namespace SurveillanceCameras.Infrastructure.Repository.Media;

public class MediaOptions
{
    public const string SectionName = "Media";

    /// <summary>URL gốc phục vụ file media (ánh xạ tới <see cref="RootDirectory"/>).</summary>
    public const string RequestPath = "/media";

    /// <summary>Thư mục lưu file media (tương đối theo thư mục chạy app).</summary>
    public string RootDirectory { get; set; } = "data/media";

    /// <summary>Dung lượng ảnh tối đa (byte).</summary>
    public long MaxImageBytes { get; set; } = 10 * 1024 * 1024;
}
