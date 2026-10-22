namespace SurveillanceCameras.Application.Repository.Tyt;

/// <summary>Trạng thái truyện trên TYT (giá trị gửi lên form: "", "1", "2").</summary>
public enum TytStoryStatus
{
    Ongoing = 0,
    Full = 1,
    Dropped = 2
}

/// <summary>Loại truyện trên TYT.</summary>
public enum TytStoryType
{
    None = 0,
    Translated = 1,
    Original = 2,
    Convert = 3
}

/// <summary>Bộ lọc của trang "Truyện Của Tôi".</summary>
public enum TytStoryVisibility
{
    All = 0,
    Public = 1,
    OnlyMe = 2,
    Deleted = 3
}

/// <summary>Một dòng trong danh sách "Truyện Của Tôi".</summary>
public record TytStoryItemDto
{
    /// <summary>Id truyện trên TYT (24 ký tự hex, tăng dần theo thời gian tạo).</summary>
    public required string Id { get; init; }

    public string? Title { get; init; }

    public string? CoverUrl { get; init; }

    /// <summary>Đã công khai (false khi trang hiện "Chưa công khai").</summary>
    public bool IsPublic { get; init; }
}

/// <summary>Thông tin truyện đọc từ trang sửa truyện.</summary>
public record TytStoryDto
{
    public required string Id { get; init; }

    public string? Title { get; init; }

    public string? Author { get; init; }

    public string? CoverUrl { get; init; }

    /// <summary>Giới thiệu dạng HTML (TYT dùng trình soạn thảo CKEditor).</summary>
    public string? DescriptionHtml { get; init; }

    public IReadOnlyList<string> Genres { get; init; } = [];

    public TytStoryStatus Status { get; init; }

    public TytStoryType Type { get; init; }
}

public record TytCreateStoryRequest
{
    /// <summary>Tối thiểu 3 ký tự; TYT yêu cầu viết hoa ký tự đầu mỗi chữ.</summary>
    public required string Title { get; init; }

    public string? Author { get; init; }
}

/// <summary>Sửa thông tin truyện. Trường để <c>null</c> thì giữ giá trị hiện tại trên TYT.</summary>
public record TytUpdateStoryRequest
{
    public string? Title { get; init; }

    public string? Author { get; init; }

    public string? CoverUrl { get; init; }

    /// <summary>Giới thiệu dạng văn bản, mỗi dòng một đoạn (được đổi sang HTML khi gửi).</summary>
    public string? Description { get; init; }

    /// <summary>Tên thể loại/tag đúng như trên TYT (vd. "Ngôn Tình", "Cổ Đại").</summary>
    public IReadOnlyList<string>? Genres { get; init; }

    public TytStoryStatus? Status { get; init; }

    public TytStoryType? Type { get; init; }
}

/// <summary>Nội dung một chương gửi lên TYT.</summary>
public record TytChapterRequest
{
    /// <summary>Tên chương (vd. "Chương 12: Tên chương").</summary>
    public required string Title { get; init; }

    /// <summary>Nội dung chương, mỗi đoạn một dòng.</summary>
    public required string Content { get; init; }

    /// <summary>Số thứ tự chương trong truyện (1–9999).</summary>
    public int Number { get; init; }

    /// <summary>Công khai cho mọi người (mặc định); <c>false</c> là "Chỉ mình tôi".</summary>
    public bool Published { get; init; } = true;
}

/// <summary>Thể loại của TYT (trang /the-loai).</summary>
public record TytGenreDto(string Slug, string Title);

/// <summary>Một chương trong danh sách chương của truyện trên TYT.</summary>
public record TytChapterItemDto(string Id, int? Number, string? Title);

/// <summary>Thu nhập của truyện (trang Nhận Thưởng), đơn vị đ.</summary>
/// <param name="Eligible">Truyện đủ điều kiện nhận thưởng (trang không còn báo "Không đủ điều kiện" và có ô bật/tắt thưởng).</param>
/// <param name="EarningEnabled">Ô "Theo Lượt Hiển Thị Quảng Cáo" đang chọn "Bật".</param>
/// <param name="Total">Nguồn thu — Tổng.</param>
/// <param name="Available">Số dư — Đang Có.</param>
/// <param name="Withdrawing">Số dư — Đang Rút.</param>
/// <param name="Paid">Số dư — Đã Thanh Toán.</param>
/// <param name="Transferred">Số dư — Đã Chuyển (về phần thưởng tài khoản).</param>
/// <param name="FromAds">Nguồn thu — Bởi Quảng Cáo.</param>
/// <param name="AdViews">Số lượt quảng cáo.</param>
public record TytEarningDto(bool Eligible, bool EarningEnabled, long? Total, long? Available, long? Withdrawing, long? Paid,
    long? Transferred, long? FromAds, long? AdViews);
