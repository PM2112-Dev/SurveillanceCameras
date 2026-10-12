namespace SurveillanceCameras.Application.Common.Exceptions;

/// <summary>Không kết nối/tải lên/xoá được ở kho lưu video (Cloudflare R2), hoặc không tài khoản nào còn chỗ.</summary>
public class VideoStorageException : Exception
{
    public VideoStorageException(string message) : base(message) { }

    public VideoStorageException(string message, Exception innerException) : base(message, innerException) { }
}
