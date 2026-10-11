using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.TikTok;
using SurveillanceCameras.Application.Repository.VideoStorage;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;

/// <summary>
/// Tải file video của tập phim từ TikTok (bằng cookie tài khoản TikTok của chủ phim) rồi lưu vào tài khoản lưu trữ (Cloudflare R2) của chủ phim, ghi link phát vào
/// <see cref="Episode.StreamUrl"/>. Video vào tài khoản đang bật có ưu tiên cao nhất còn đủ chỗ; đầy (hoặc lỗi) thì
/// chuyển sang tài khoản kế tiếp.
/// </summary>
public class EpisodeVideoStorer
{
    private const int MaxErrorLength = 2000;

    private readonly IApplicationDbContext _context;
    private readonly ITikTokRepository _tikTok;
    private readonly IVideoStorageRepository _storage;
    private readonly ISecretProtector _protector;
    private readonly EpisodeVideoStoreOptions _options;
    private readonly ILogger<EpisodeVideoStorer> _logger;

    // Cookie TikTok theo chủ phim, đọc một lần cho mỗi lượt (đối tượng sống theo scope).
    private readonly Dictionary<string, string?> _cookies = [];

    public EpisodeVideoStorer(IApplicationDbContext context, ITikTokRepository tikTok, IVideoStorageRepository storage,
        ISecretProtector protector, IOptions<EpisodeVideoStoreOptions> options, ILogger<EpisodeVideoStorer> logger)
    {
        _context = context;
        _tikTok = tikTok;
        _storage = storage;
        _protector = protector;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Lưu video cho một lô tập đang chờ (chưa có link phát, chưa lỗi quá số lần) của những người dùng còn chỗ lưu.</summary>
    /// <returns>Số tập đã thử lưu trong lượt này (0 nghĩa là không còn gì để làm).</returns>
    /// <exception cref="TikTokBlockedException">TikTok đang chặn máy chủ: dừng lô, bên gọi nên nghỉ lâu rồi mới thử tiếp.</exception>
    public async Task<int> StoreNextBatchAsync(CancellationToken cancellationToken)
    {
        // Người dùng không còn tài khoản nào trống thì không tải video của họ về (tải xong cũng không có chỗ để).
        var owners = await _context.StorageAccounts
            .Where(x => x.IsEnabled && x.UsedBytes < x.QuotaBytes)
            .Select(x => x.CreatedBy)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (owners.Count == 0) return 0;

        var episodes = await _context.Episodes
            .Where(x => x.Work!.Type == WorkType.Movie && owners.Contains(x.Work.CreatedBy)
                        && x.StreamUrl == null && x.VideoUrl != null && x.StorageAttempts < _options.MaxAttempts)
            // Tập chưa thử lần nào đi trước để một tập lỗi không chặn cả hàng.
            .OrderBy(x => x.StorageAttempts).ThenBy(x => x.WorkId).ThenBy(x => x.EpisodeNumber)
            .Take(Math.Max(1, _options.BatchSize))
            .Include(x => x.Work)
            .ToListAsync(cancellationToken);

        var delay = TimeSpan.FromSeconds(Math.Max(0, _options.DelayBetweenVideosSeconds));
        for (var i = 0; i < episodes.Count; i++)
        {
            if (i > 0 && delay > TimeSpan.Zero) await Task.Delay(delay, cancellationToken);
            await StoreAsync(episodes[i], cancellationToken);
        }

        return episodes.Count;
    }

    /// <summary>Tải và lưu video của một tập; lỗi được ghi vào tập (<see cref="Episode.StorageError"/>) chứ không ném ra.</summary>
    /// <exception cref="TikTokBlockedException">TikTok đang chặn máy chủ (đã ghi vào tập, không tính là một lần thử).</exception>
    public async Task StoreAsync(Episode episode, CancellationToken cancellationToken)
    {
        var temp = Path.Combine(Path.GetTempPath(), $"episode-{episode.Id}-{Guid.NewGuid():N}.mp4");
        try
        {
            var cookie = await TikTokCookieAsync(episode.Work!.CreatedBy, cancellationToken);
            var size = await _tikTok.DownloadVideoAsync(cookie, episode.VideoUrl!, temp, _options.MaxVideoBytes, cancellationToken);
            var account = await UploadAsync(episode, temp, size, cancellationToken);

            episode.StreamUrl = account.PublicUrl(episode.StorageKey!);
            episode.StorageAccountId = account.Id;
            episode.StorageSizeBytes = size;
            episode.StorageError = null;
            account.UsedBytes += size;
            account.LastError = null;
        }
        catch (TikTokBlockedException ex)
        {
            // Không phải lỗi của tập: giữ nguyên số lần thử để tập được làm lại khi TikTok thôi chặn.
            episode.StorageError = ex.Message;
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }
        catch (Exception ex) when (ex is TikTokApiException or VideoStorageException or IOException)
        {
            _logger.LogWarning("Tập {EpisodeId}: không lưu được video: {Error}", episode.Id, ex.Message);
            episode.StorageKey = null;
            episode.StorageError = ex.Message.Length <= MaxErrorLength ? ex.Message : ex.Message[..MaxErrorLength];
            // TikTok không cấp file thì thử lại cũng vậy: thôi luôn, tới khi người dùng bấm thử lại.
            episode.StorageAttempts = ex is TikTokVideoUnavailableException ? _options.MaxAttempts : episode.StorageAttempts + 1;
        }
        finally
        {
            try
            {
                File.Delete(temp);
            }
            catch (IOException)
            {
                // File tạm còn sót sẽ do hệ điều hành dọn.
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Cookie của tài khoản TikTok mà chủ phim đăng nhập gần nhất (loại tài khoản có trang đăng nhập thuộc TikTok);
    /// <c>null</c> khi chưa có — khi đó chỉ tải được video TikTok thường, không tải được tập drama.
    /// </summary>
    private async Task<string?> TikTokCookieAsync(string? owner, CancellationToken cancellationToken)
    {
        if (_cookies.TryGetValue(owner ?? "", out var cached)) return cached;

        var cookie = await _context.Accounts
            .Where(x => x.CreatedBy == owner && x.Cookie != null && x.Cookie != ""
                        && x.AccountType!.BaseUrl != null && x.AccountType.BaseUrl.Contains("tiktok.com"))
            .OrderByDescending(x => x.LastModified)
            .Select(x => x.Cookie)
            .FirstOrDefaultAsync(cancellationToken);

        return _cookies[owner ?? ""] = cookie;
    }

    /// <summary>Tải file lên tài khoản đầu tiên còn đủ chỗ; tài khoản lỗi thì thử tài khoản kế tiếp.</summary>
    /// <returns>Tài khoản đã nhận file (khoá file được ghi vào <see cref="Episode.StorageKey"/>).</returns>
    private async Task<StorageAccount> UploadAsync(Episode episode, string filePath, long size, CancellationToken cancellationToken)
    {
        var owner = episode.Work!.CreatedBy;
        var accounts = await _context.StorageAccounts
            .Where(x => x.CreatedBy == owner && x.IsEnabled)
            .ToListAsync(cancellationToken);

        var candidates = Candidates(accounts, size);
        if (candidates.Count == 0)
        {
            throw new VideoStorageException(
                $"Không tài khoản lưu trữ nào còn đủ {size / 1024d / 1024d:0.#} MB trống. Thêm tài khoản mới hoặc nâng hạn mức rồi bấm thử lại.");
        }

        // Tên file có phần ngẫu nhiên: bucket công khai nên không để đoán được link các tập bị khoá.
        var key = $"movies/{episode.WorkId}/{episode.Id}-{Guid.NewGuid():N}.mp4";
        var errors = new List<string>();
        foreach (var account in candidates)
        {
            try
            {
                await _storage.UploadFileAsync(account.ToTarget(_protector), key, filePath, "video/mp4", cancellationToken);
                episode.StorageKey = key;
                return account;
            }
            catch (VideoStorageException ex)
            {
                _logger.LogWarning("Tài khoản lưu trữ {AccountId}: tải lên lỗi: {Error}", account.Id, ex.Message);
                account.LastError = ex.Message.Length <= MaxErrorLength ? ex.Message : ex.Message[..MaxErrorLength];
                errors.Add($"{account.Title}: {ex.Message}");
            }
        }

        throw new VideoStorageException("Tải lên kho lỗi — " + string.Join("; ", errors));
    }

    /// <summary>Các tài khoản đang bật còn đủ chỗ cho file <paramref name="size"/> byte, theo thứ tự được dùng.</summary>
    public static List<StorageAccount> Candidates(IEnumerable<StorageAccount> accounts, long size)
        => accounts
            .Where(x => x.IsEnabled && x.FreeBytes >= size)
            .OrderBy(x => x.Priority).ThenBy(x => x.Id)
            .ToList();

    /// <summary>
    /// Gỡ file video đã lưu của các tập (trước khi xoá tập hoặc thay link phát): xoá file trong bucket, trả lại dung lượng
    /// cho tài khoản và xoá thông tin lưu trữ trên tập. Chưa gọi SaveChanges. Xoá file lỗi thì chỉ ghi log — file mồ côi
    /// sẽ lộ ra khi đối soát dung lượng tài khoản.
    /// </summary>
    public async Task ReleaseAsync(IReadOnlyCollection<Episode> episodes, CancellationToken cancellationToken)
    {
        var stored = episodes.Where(x => x.StorageAccountId is not null && x.StorageKey is not null).ToList();
        if (stored.Count == 0) return;

        var accountIds = stored.Select(x => x.StorageAccountId!.Value).Distinct().ToList();
        var accounts = await _context.StorageAccounts
            .Where(x => accountIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var episode in stored)
        {
            if (accounts.TryGetValue(episode.StorageAccountId!.Value, out var account))
            {
                try
                {
                    await _storage.DeleteAsync(account.ToTarget(_protector), episode.StorageKey!, cancellationToken);
                }
                catch (VideoStorageException ex)
                {
                    _logger.LogWarning("Tập {EpisodeId}: không xoá được file {Key} ở tài khoản lưu trữ {AccountId}: {Error}",
                        episode.Id, episode.StorageKey, account.Id, ex.Message);
                }

                account.UsedBytes = Math.Max(0, account.UsedBytes - (episode.StorageSizeBytes ?? 0));
            }

            episode.StreamUrl = null;
            episode.StorageAccountId = null;
            episode.StorageKey = null;
            episode.StorageSizeBytes = null;
        }
    }
}
