namespace SurveillanceCameras.Domain.Enums;

/// <summary>Nền tảng cung cấp model AI. Giá trị số được lưu trong DB — không đổi số của mục đã có.</summary>
public enum AIProvider
{
    Gemini = 1,
    Claude = 2,
    ChatGpt = 3,
    DeepSeek = 4,
    Grok = 5
}
