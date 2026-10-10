namespace SurveillanceCameras.Application.Local.AIs;

/// <summary>Quy tắc dùng chung cho lệnh tạo/sửa AI.</summary>
internal static class AIValidation
{
    public const int MaxModelLength = 200;
    public const int MaxApiKeyLength = 500;

    public static bool BeValidEmail(string? email)
        => string.IsNullOrWhiteSpace(email) || System.Net.Mail.MailAddress.TryCreate(email.Trim(), out _);

    public static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
