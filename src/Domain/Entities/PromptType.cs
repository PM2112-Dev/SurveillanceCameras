namespace SurveillanceCameras.Domain.Entities;

/// <summary>
/// Loại prompt: dùng cho tác vụ nào (biên tập tên truyện, giới thiệu, nội dung chương…).
/// <c>Code</c> là mã cố định để hệ thống chọn prompt theo tác vụ (xem <c>PromptTypeCodes</c>).
/// </summary>
public class PromptType : BaseAuditableEntity
{
    /// <summary>Mô tả khi nào dùng loại prompt này.</summary>
    public string? Description { get; set; }

    public ICollection<Prompt>? Prompts { get; init; }
}
