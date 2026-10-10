namespace SurveillanceCameras.Domain.Entities;

public class Prompt : BaseAuditableEntity
{
    public int PromptTypeId { get; set; }

    /// <summary>Nội dung prompt gửi cho AI.</summary>
    public string? Content { get; set; }

    public PromptType? PromptType { get; set; }
}
