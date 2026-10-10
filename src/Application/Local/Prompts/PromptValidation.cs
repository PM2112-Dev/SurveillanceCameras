using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Prompts;

internal static class PromptValidation
{
    /// <summary>Loại prompt phải tồn tại và thuộc về người dùng hiện tại.</summary>
    public static Task<bool> PromptTypeExistsAsync(IApplicationDbContext context, IUser user, int promptTypeId,
        CancellationToken cancellationToken)
        => context.PromptTypes.AnyAsync(x => x.Id == promptTypeId && x.CreatedBy == user.Id, cancellationToken);
}
