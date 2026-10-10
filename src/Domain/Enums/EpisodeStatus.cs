namespace SurveillanceCameras.Domain.Enums;

/// <summary>Tiến trình xử lý một chương lấy từ trang nguồn. Giá trị số được lưu trong DB.</summary>
public enum EpisodeStatus
{
    /// <summary>Có trong mục lục, chưa tải nội dung (hoặc chương nhập tay).</summary>
    Listed = 1,

    /// <summary>Đã tải nội dung gốc, chờ AI biên tập (hoặc chưa có prompt biên tập).</summary>
    Fetched = 2,

    /// <summary>AI đã biên tập nội dung.</summary>
    Edited = 3,

    FetchFailed = 4,

    EditFailed = 5
}
