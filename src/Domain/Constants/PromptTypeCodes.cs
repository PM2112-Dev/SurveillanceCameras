namespace SurveillanceCameras.Domain.Constants;

/// <summary>
/// Mã (<c>PromptType.Code</c>) của các tác vụ hệ thống dùng prompt. Code tìm prompt theo mã này nên không đổi giá trị
/// khi đã có dữ liệu; tác vụ mới thì bổ sung hằng số ở đây.
/// </summary>
public abstract class PromptTypeCodes
{
    /// <summary>Biên tập tên truyện.</summary>
    public const string EditTitle = "edit-title";

    /// <summary>Biên tập giới thiệu truyện.</summary>
    public const string EditDescription = "edit-description";

    /// <summary>Biên tập nội dung chương (bản dịch word-by-word → tiếng Việt tự nhiên).</summary>
    public const string EditChapterContent = "edit-chapter-content";
}
