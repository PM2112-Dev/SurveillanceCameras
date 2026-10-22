using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Movies;

/// <summary>
/// Phát hiện tập phim bị lỗi / trùng. "Trùng" là nhiều tập cùng số tập (cùng tên "Tập N") — thường do TikTok đăng lại
/// tập với ID video mới hoặc do sửa số tập nhầm. Hai tập trùng số mà thời lượng gần nhau (hoặc thiếu thời lượng) coi là bản
/// sao của nhau; thời lượng khác hẳn là hai nội dung khác nhau bị đánh cùng số (xung đột, cần đánh số lại).
/// </summary>
public static class MovieEpisodeIssues
{
    /// <summary>Hai tập cùng số có thời lượng chênh không quá bấy nhiêu giây thì coi là bản sao.</summary>
    public const int SameDurationToleranceSeconds = 2;

    public record Info(int Id, int EpisodeNumber, int? DurationSeconds, string? ExternalId, string? VideoUrl, string? StreamUrl,
        string? StorageError, bool IsEdited);

    public static Info ToInfo(Episode x)
        => new(x.Id, x.EpisodeNumber, x.DurationSeconds, x.ExternalId, x.VideoUrl, x.StreamUrl, x.StorageError, x.IsEdited);

    /// <summary>Tập không còn nguồn nào để phát: không link TikTok, không file tự lưu.</summary>
    public static bool IsMissingSource(Info x) => string.IsNullOrWhiteSpace(x.VideoUrl) && string.IsNullOrWhiteSpace(x.StreamUrl);

    /// <summary>Lưu video đang lỗi (chưa có file phát được).</summary>
    public static bool IsStorageFailed(Info x) => x.StorageError is not null && string.IsNullOrWhiteSpace(x.StreamUrl);

    /// <summary>Tập nào nên giữ trong một nhóm bản sao: có file phát, rồi tới tập đã sửa tay, rồi tập cũ nhất.</summary>
    public static Info Keeper(IEnumerable<Info> group)
        => group.OrderByDescending(x => !string.IsNullOrWhiteSpace(x.StreamUrl))
            .ThenByDescending(x => x.IsEdited)
            .ThenByDescending(x => !string.IsNullOrWhiteSpace(x.VideoUrl))
            .ThenBy(x => x.Id)
            .First();

    /// <summary>Các nhóm tập cùng số tập (từ 2 tập trở lên).</summary>
    public static List<List<Info>> DuplicateNumberGroups(IEnumerable<Info> episodes)
        => episodes.GroupBy(x => x.EpisodeNumber).Where(g => g.Count() > 1).Select(g => g.OrderBy(x => x.Id).ToList()).ToList();

    /// <summary>
    /// Tách một nhóm cùng số tập thành các cụm bản sao (thời lượng gần nhau). Mỗi cụm có &gt; 1 tập là bản sao; nhóm có nhiều
    /// hơn một cụm là xung đột (cùng số nhưng nội dung khác nhau).
    /// </summary>
    public static List<List<Info>> ClusterBySameContent(List<Info> group)
    {
        var clusters = new List<List<Info>>();
        foreach (var item in group)
        {
            var cluster = clusters.FirstOrDefault(c => IsSameContent(c[0], item));
            if (cluster is null) clusters.Add([item]);
            else cluster.Add(item);
        }

        return clusters;
    }

    public static bool IsSameContent(Info a, Info b)
        => a.DurationSeconds is null || b.DurationSeconds is null
           || Math.Abs(a.DurationSeconds.Value - b.DurationSeconds.Value) <= SameDurationToleranceSeconds;
}
