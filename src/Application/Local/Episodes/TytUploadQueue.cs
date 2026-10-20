using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Episodes;

/// <summary>Đưa chương vào hàng đợi đăng lên TYT, theo đúng thứ tự chương.</summary>
internal static class TytUploadQueue
{
    /// <summary>Chương đã vào hàng đợi trong khoảng này mà chưa có kết quả được coi là đang xử lý, chưa đưa vào lại.</summary>
    public static readonly TimeSpan InFlight = TimeSpan.FromMinutes(30);

    /// <summary>Số chương xét mỗi lượt cho một truyện — đủ rộng cho lô lớn mà không nạp cả nghìn chương.</summary>
    private const int Window = 500;

    /// <summary>Truyện đã sẵn sàng nhận chương: đã có trên TYT và có tài khoản đăng.</summary>
    public static bool CanUpload(Work work)
        => work.Type == WorkType.Story && !string.IsNullOrWhiteSpace(work.TytStoryId) && work.AccountId is not null;

    /// <summary>
    /// Đưa tối đa <paramref name="take"/> chương kế tiếp (theo số chương) của truyện vào hàng đợi. Dừng ở chương đầu tiên
    /// chưa xử lý xong hoặc đang lỗi đăng, để chương sau không lên TYT trước chương trước. Chương đang trong hàng đợi
    /// tính vào lô nhưng không đưa vào lần nữa.
    /// </summary>
    /// <returns>Số chương mới được đưa vào hàng đợi.</returns>
    public static async Task<int> QueueNextAsync(IApplicationDbContext db, Work work, int take, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (take <= 0 || !CanUpload(work)) return 0;

        var pending = await db.Episodes
            .AsNoTracking()
            .Where(x => x.WorkId == work.Id && !x.IsUploaded)
            .OrderBy(x => x.EpisodeNumber).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                Ready = x.Status == EpisodeStatus.Edited && x.ContentEdit != null && x.ContentEdit != "",
                x.TytUploadError,
                x.QueuedAt
            })
            .Take(Window)
            .ToListAsync(cancellationToken);

        var toQueue = new List<int>();
        var counted = 0;
        foreach (var chapter in pending)
        {
            if (counted >= take || !chapter.Ready || chapter.TytUploadError is not null) break;
            counted++;
            if (chapter.QueuedAt is null || now - chapter.QueuedAt.Value >= InFlight) toQueue.Add(chapter.Id);
        }

        if (toQueue.Count == 0) return 0;

        var chapters = await db.Episodes.Where(x => toQueue.Contains(x.Id)).ToListAsync(cancellationToken);
        foreach (var chapter in chapters)
        {
            chapter.QueuedAt = now;
            chapter.AddIntegrationEvent(new ChapterTytUploadRequestedIntegrationEvent { ChapterId = chapter.Id }, cancellationToken);
        }

        return chapters.Count;
    }

    /// <summary>
    /// Chương vừa xử lý xong: truyện đặt đăng ngay thì đăng luôn, miễn mọi chương trước đó đã lên TYT (nếu chưa, lượt quét
    /// theo thứ tự sẽ đăng). Không lưu — người gọi lưu cùng thay đổi của chương.
    /// </summary>
    public static async Task QueueIfImmediateAsync(IApplicationDbContext db, Episode chapter, Work? work, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (work is not { TytUploadMode: TytUploadMode.Immediate } || !CanUpload(work) || chapter.IsUploaded) return;

        var blocked = await db.Episodes.AnyAsync(
            x => x.WorkId == chapter.WorkId && !x.IsUploaded && x.Id != chapter.Id && x.EpisodeNumber < chapter.EpisodeNumber,
            cancellationToken);
        if (blocked) return;

        chapter.QueuedAt = now;
        chapter.AddIntegrationEvent(new ChapterTytUploadRequestedIntegrationEvent { ChapterId = chapter.Id }, cancellationToken);
    }
}
