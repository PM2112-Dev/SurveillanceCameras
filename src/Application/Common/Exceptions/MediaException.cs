namespace SurveillanceCameras.Application.Common.Exceptions;

/// <summary>Không tải/lưu được file media (ảnh bìa…).</summary>
public class MediaException : Exception
{
    public MediaException(string message) : base(message) { }

    public MediaException(string message, Exception innerException) : base(message, innerException) { }
}
