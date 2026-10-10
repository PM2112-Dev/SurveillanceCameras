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
