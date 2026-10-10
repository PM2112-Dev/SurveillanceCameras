using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.WebSources;

internal static class WebSourceEditPolicy
{
    /// <summary>
    /// Truyện từ nguồn web này có cần AI biên tập không (<see cref="WebSource.IsNeedEdit"/>). Không xác định được nguồn
    /// (truyện nhập tay, nguồn đã xoá) thì coi là cần — giữ hành vi mặc định.
    /// </summary>
    public static async Task<bool> IsNeedEditAsync(IApplicationDbContext context, int? webSourceId,
        CancellationToken cancellationToken)
    {
        if (webSourceId is null) return true;

        var isNeedEdit = await context.WebSources
            .Where(x => x.Id == webSourceId)
            .Select(x => (bool?)x.IsNeedEdit)
            .FirstOrDefaultAsync(cancellationToken);

        return isNeedEdit ?? true;
    }
}
