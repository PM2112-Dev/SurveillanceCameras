namespace SurveillanceCameras.Domain.Entities;

/// <summary>Một API key của một nền tảng AI, kèm model dùng với key đó.</summary>
public class AI : BaseAuditableEntity
{
    public AIProvider Provider { get; set; } = AIProvider.Gemini;

    /// <summary>ID model của nền tảng (vd. <c>gemini-2.5-flash</c>, <c>claude-opus-5</c>).</summary>
    public string? Model { get; set; }

    public string? ApiKey { get; set; }

    /// <summary>Project mà API key thuộc về (Google Cloud project, OpenAI project…).</summary>
    public string? Project { get; set; }

    /// <summary>Tài khoản (email) sở hữu API key.</summary>
    public string? Email { get; set; }
}
