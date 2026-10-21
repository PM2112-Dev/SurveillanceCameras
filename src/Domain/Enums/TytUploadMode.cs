namespace SurveillanceCameras.Domain.Enums;

/// <summary>Khi nào chương đã xử lý xong (tải + biên tập) được đưa lên TYT. Giá trị số được lưu trong DB.</summary>
public enum TytUploadMode
{
    /// <summary>Không tự đăng; người dùng bấm đăng chương.</summary>
    Manual = 0,

    /// <summary>Xử lý xong chương nào đăng ngay chương đó.</summary>
    Immediate = 1,

    /// <summary>Cứ mỗi N giờ đăng một lô chương.</summary>
    Hourly = 2,

    /// <summary>Mỗi ngày vào một giờ cố định đăng một lô chương.</summary>
    Daily = 3
}
