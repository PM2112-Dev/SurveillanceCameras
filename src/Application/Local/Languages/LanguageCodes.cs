namespace SurveillanceCameras.Application.Local.Languages;

public static class LanguageCodes
{
    public const int MaxCodeLength = 20;

    // Tên hiển thị mặc định khi hệ thống tự tạo ngôn ngữ lúc đồng bộ; người dùng sửa được ở mục Ngôn ngữ.
    private static readonly Dictionary<string, string> Names = new()
    {
        ["vi"] = "Tiếng Việt", ["en"] = "Tiếng Anh", ["zh"] = "Tiếng Trung", ["zh-hans"] = "Tiếng Trung (giản thể)",
        ["zh-hant"] = "Tiếng Trung (phồn thể)", ["ja"] = "Tiếng Nhật", ["ko"] = "Tiếng Hàn", ["th"] = "Tiếng Thái",
        ["id"] = "Tiếng Indonesia", ["ms"] = "Tiếng Mã Lai", ["tl"] = "Tiếng Philippines", ["hi"] = "Tiếng Hindi",
        ["es"] = "Tiếng Tây Ban Nha", ["pt"] = "Tiếng Bồ Đào Nha", ["fr"] = "Tiếng Pháp", ["de"] = "Tiếng Đức",
        ["it"] = "Tiếng Ý", ["ru"] = "Tiếng Nga", ["ar"] = "Tiếng Ả Rập", ["tr"] = "Tiếng Thổ Nhĩ Kỳ"
    };

    /// <summary>
    /// Mã ngôn ngữ dạng chuẩn để so khớp (chữ thường, bỏ khoảng trắng); <c>null</c> nếu trống, quá dài hoặc là
    /// <c>un</c> — giá trị TikTok trả khi không nhận diện được ngôn ngữ.
    /// </summary>
    public static string? Normalize(string? code)
    {
        var value = code?.Trim().ToLowerInvariant();
        return string.IsNullOrEmpty(value) || value == "un" || value.Length > MaxCodeLength ? null : value;
    }

    public static string DisplayName(string code) => Names.GetValueOrDefault(code, code);
}
