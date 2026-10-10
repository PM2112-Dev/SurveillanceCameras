namespace SurveillanceCameras.Application.Repository.Media;

/// <summary>Lưu ảnh (bìa phim…) về kho của hệ thống để không phụ thuộc link có hạn của nền tảng nguồn.</summary>
public interface IMediaRepository
{
    /// <summary>Tải ảnh từ <paramref name="sourceUrl"/> và lưu vào <paramref name="folder"/> với tên <paramref name="name"/>.</summary>
    /// <returns>URL tương đối để hiển thị (vd. <c>/media/covers/movies/12.jpg</c>).</returns>
    /// <exception cref="Common.Exceptions.MediaException">Tải lỗi, không phải ảnh hoặc ảnh quá lớn.</exception>
    Task<string> SaveImageFromUrlAsync(string sourceUrl, string folder, string name, CancellationToken cancellationToken = default);

    /// <summary>URL là ảnh đã lưu trong kho và file vẫn còn.</summary>
    bool IsStored(string? url);
}
