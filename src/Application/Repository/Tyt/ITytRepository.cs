namespace SurveillanceCameras.Application.Repository.Tyt;

/// <summary>
/// Quản lý truyện của tài khoản trên TYT (trang "Truyện Của Tôi"). TYT nằm sau Cloudflare nên mọi request đi qua phiên
/// Chrome của tài khoản (xem <c>IAccountSessionManagerRepository.FetchAsync</c>); tài khoản phải đăng nhập TYT trước.
/// Lỗi được ném dưới dạng <see cref="Common.Exceptions.TytException"/>.
/// </summary>
public interface ITytRepository
{
    /// <summary>Thêm truyện mới (chỉ tên và tác giả — thông tin khác sửa bằng <see cref="UpdateStoryAsync"/>).</summary>
    /// <returns>Id truyện vừa tạo trên TYT.</returns>
    Task<string> CreateStoryAsync(TytContext context, TytCreateStoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Danh sách truyện của tài khoản, cũ nhất trước.</summary>
    Task<IReadOnlyList<TytStoryItemDto>> GetStoriesAsync(TytContext context,
        TytStoryVisibility visibility = TytStoryVisibility.All, CancellationToken cancellationToken = default);

    /// <summary>Thông tin hiện tại của một truyện (đọc từ trang sửa truyện).</summary>
    Task<TytStoryDto> GetStoryAsync(TytContext context, string storyId, CancellationToken cancellationToken = default);

    /// <summary>Sửa thông tin truyện; trường không truyền giữ nguyên giá trị đang có trên TYT.</summary>
    Task UpdateStoryAsync(TytContext context, string storyId, TytUpdateStoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm Id truyện theo tên (không phân biệt hoa thường, bỏ khoảng trắng thừa). Có nhiều truyện trùng tên thì lấy
    /// truyện tạo gần nhất. Không có thì trả về <c>null</c>.
    /// </summary>
    Task<string?> GetStoryIdAsync(TytContext context, string title, CancellationToken cancellationToken = default);
}
