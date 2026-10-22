using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Tyt;

/// <summary>Số truyện đang đăng của một tài khoản TYT (đã có trên TYT, chưa hoàn thành) — truyện hoàn thành không chiếm chỗ.</summary>
internal static class TytSlots
{
    public static Task<int> ActiveCountAsync(IApplicationDbContext db, int accountId, int? exceptWorkId, CancellationToken cancellationToken)
        => db.Works.CountAsync(x => x.Type == WorkType.Story && x.AccountId == accountId && x.TytStoryId != null && x.TytStoryId != ""
                                    && !x.IsFull && (exceptWorkId == null || x.Id != exceptWorkId), cancellationToken);
}
