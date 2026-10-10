namespace SurveillanceCameras.Application.Repository.CrawlStory.DTOs;

/// <summary>Một trang dữ liệu lấy từ trang nguồn. Tổng số trang có thể không biết (trang nguồn không cho).</summary>
public class CrawlPageDto<T>
{
    public List<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int? TotalPages { get; init; }

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage { get; init; }

    /// <summary>URL đã tải — tiện dò lỗi cấu hình.</summary>
    public string? SourceUrl { get; init; }
}
