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

    /// <summary>
    /// Kiểm tra phiên đăng nhập TYT của tài khoản còn dùng được không (một request nhẹ tới trang quản lý truyện).
    /// </summary>
    /// <exception cref="Common.Exceptions.TytException">Chưa đăng nhập, phiên hết hạn hoặc TYT từ chối/lỗi.</exception>
    Task CheckSessionAsync(TytContext context, CancellationToken cancellationToken = default);

    /// <summary>Danh sách thể loại của TYT (đọc từ trang /the-loai).</summary>
    Task<IReadOnlyList<TytGenreDto>> GetGenresAsync(TytContext context, CancellationToken cancellationToken = default);

    /// <summary>Đăng chương mới vào truyện.</summary>
    /// <returns>Id chương vừa tạo trên TYT; <c>null</c> nếu TYT không trả về (chương vẫn đã được đăng).</returns>
    Task<string?> CreateChapterAsync(TytContext context, string storyId, TytChapterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Sửa nội dung chương đã đăng.</summary>
    Task UpdateChapterAsync(TytContext context, string storyId, string chapterId, TytChapterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi yêu cầu công khai truyện (trang Xuất bản): TYT cần xét duyệt nên truyện chưa hiện công khai ngay — dùng
    /// <see cref="GetStoriesAsync"/> với <see cref="TytStoryVisibility.Public"/> để biết đã được duyệt chưa.
    /// </summary>
    /// <returns>Thông điệp TYT trả về (vd. đã gửi yêu cầu, đang chờ duyệt).</returns>
    Task<string?> RequestPublishAsync(TytContext context, string storyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Truyện đã công khai chưa, theo ô chế độ hiển thị ở trang Xuất bản (lựa chọn đang chọn là "Công Khai"). Dùng để biết TYT
    /// đã duyệt truyện chưa sau khi gửi yêu cầu công khai.
    /// </summary>
    Task<bool> IsStoryPublicAsync(TytContext context, string storyId, CancellationToken cancellationToken = default);

    /// <summary>Các chương của truyện trên TYT (mọi trang), dùng để tìm Id chương và tránh đăng trùng số chương.</summary>
    Task<IReadOnlyList<TytChapterItemDto>> GetChaptersAsync(TytContext context, string storyId, CancellationToken cancellationToken = default);
}
