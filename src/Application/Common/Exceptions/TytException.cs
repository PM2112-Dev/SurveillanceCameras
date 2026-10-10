namespace SurveillanceCameras.Application.Common.Exceptions;

/// <summary>Thao tác trên TYT thất bại (chưa đăng nhập, bị chặn, TYT báo lỗi dữ liệu…).</summary>
public class TytException : Exception
{
    public TytException(string message) : base(message) { }

    public TytException(string message, Exception innerException) : base(message, innerException) { }
}
