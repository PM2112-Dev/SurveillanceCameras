using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Episodes;

internal static class EpisodeContentStatus
{
    /// <summary>Trạng thái theo nội dung khi người dùng tự nhập/sửa chương.</summary>
    public static EpisodeStatus From(string? contentRaw, string? contentEdit)
        => !string.IsNullOrWhiteSpace(contentEdit) ? EpisodeStatus.Edited
            : !string.IsNullOrWhiteSpace(contentRaw) ? EpisodeStatus.Fetched
            : EpisodeStatus.Listed;

    /// <summary>
    /// Nguồn không cần AI biên tập: dùng luôn nội dung gốc làm nội dung biên tập để các bước sau (đăng truyện, video)
    /// chỉ cần đọc <see cref="Episode.ContentEdit"/>. <see cref="Episode.PromptId"/> để trống cho biết không qua AI.
    /// </summary>
    public static void KeepOriginal(Episode chapter, DateTimeOffset now)
    {
        chapter.ContentEdit = chapter.ContentRaw;
        chapter.PromptId = null;
        chapter.Status = EpisodeStatus.Edited;
        chapter.EditedAt = now;
        chapter.Error = null;
    }
}
