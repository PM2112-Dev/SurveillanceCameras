using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.PromptTypes;

/// <summary>Quy tắc dùng chung cho lệnh tạo/sửa loại prompt.</summary>
internal static class PromptTypeValidation
{
    /// <summary>Mã chuẩn hoá: chữ thường, bỏ khoảng trắng đầu/cuối.</summary>
    public static string? NormalizeCode(string? code) => string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToLowerInvariant();

    /// <summary>Mã chưa dùng cho loại prompt khác của cùng người dùng (bỏ qua chính <paramref name="exceptId"/>).</summary>
    public static async Task<bool> IsCodeAvailableAsync(IApplicationDbContext context, IUser user, string? code, int? exceptId,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeCode(code);
        return !await context.PromptTypes.AnyAsync(
            x => x.CreatedBy == user.Id && x.Code == normalized && x.Id != exceptId, cancellationToken);
    }
}
