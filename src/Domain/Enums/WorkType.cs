namespace SurveillanceCameras.Domain.Enums;

/// <summary>Loại tác phẩm. Giá trị số được lưu trong DB.</summary>
public enum WorkType
{
    /// <summary>Truyện chữ: các <c>Episode</c> là chương (nội dung chữ).</summary>
    Story = 1,

    /// <summary>Phim: các <c>Episode</c> là tập phim (link video).</summary>
    Movie = 2
}
