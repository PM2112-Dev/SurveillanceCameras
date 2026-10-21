using System.Collections.Concurrent;

namespace SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;

/// <summary>
/// Link file các tập của drama TikTok (id video → các link), nhớ theo (chủ phim, drama) trong ít phút. Mỗi tập được xử lý
/// ở một lượt riêng (Kafka) nên không có cache này thì mỗi tập lại đọc cả danh sách tập của drama từ TikTok. Link TikTok
/// sống khoảng 48 giờ nên giữ ngắn là đủ an toàn.
/// </summary>
public class DramaFileCache(TimeProvider timeProvider)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(20);

    private readonly ConcurrentDictionary<(string Owner, string DramaId), (DateTimeOffset At, Dictionary<string, List<string>> Files)> _items = new();

    public Dictionary<string, List<string>>? TryGet(string? owner, string dramaId)
        => _items.TryGetValue((owner ?? "", dramaId), out var item) && timeProvider.GetUtcNow() - item.At < Ttl ? item.Files : null;

    public void Set(string? owner, string dramaId, Dictionary<string, List<string>> files)
        => _items[(owner ?? "", dramaId)] = (timeProvider.GetUtcNow(), files);

    public void Remove(string? owner, string dramaId) => _items.TryRemove((owner ?? "", dramaId), out _);
}
