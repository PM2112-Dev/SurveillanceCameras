using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models.TikTok;
using SurveillanceCameras.Application.Local.Movies.IntegrationEvents;
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
    private const int EpisodePageSize = 30;

    /// <summary>Chặn vòng lặp nếu TikTok trả hasMore mãi.</summary>
    private const int MaxEpisodePages = 50;

    private readonly IApplicationDbContext _context;
    private readonly ITikTokRepository _tikTok;
    private readonly IVideoStorageRepository _storage;
    private readonly ISecretProtector _protector;
    private readonly EpisodeVideoStoreOptions _options;
    private readonly ILogger<EpisodeVideoStorer> _logger;

    // Cookie TikTok theo chủ phim, đọc một lần cho mỗi lượt (đối tượng sống theo scope).
    private readonly Dictionary<string, string?> _cookies = [];

    // Link file các tập theo (chủ phim, drama), dùng chung giữa các tập của cùng một drama — mỗi tập một lượt xử lý riêng
    // nên không đọc lại danh sách tập của TikTok cho từng tập.
    private readonly DramaFileCache _dramaFiles;

    public EpisodeVideoStorer(IApplicationDbContext context, ITikTokRepository tikTok, IVideoStorageRepository storage,
        ISecretProtector protector, IOptions<EpisodeVideoStoreOptions> options, DramaFileCache dramaFiles,
        ILogger<EpisodeVideoStorer> logger)
    {
        _dramaFiles = dramaFiles;
        _context = context;
        _tikTok = tikTok;
        _storage = storage;
        _protector = protector;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Đưa các tập chưa có video trên kho vào hàng đợi Kafka (sự kiện <see cref="EpisodeVideoStoreRequestedIntegrationEvent"/>).
    /// Mỗi người dùng chỉ được đưa vào khi tài khoản lưu trữ của họ còn ít nhất <see cref="EpisodeVideoStoreOptions.MinFreeBytes"/>,
    /// và số tập không vượt quá số video kho còn chứa được (theo dung lượng trung bình một video), tính cả các tập đang chờ
    /// trong hàng đợi — tải xong mà không có chỗ để thì phí cả lượt tải từ TikTok.
    /// </summary>
    /// <returns>Số tập được đưa vào hàng đợi.</returns>
    public async Task<int> QueuePendingAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var requeueBefore = now - TimeSpan.FromMinutes(Math.Max(1, _options.RequeueAfterMinutes));
        var exhaustedBefore = _options.ExhaustedRetryHours > 0 ? now - TimeSpan.FromHours(_options.ExhaustedRetryHours) : (DateTimeOffset?)null;
        var max = Math.Max(1, _options.MaxAttempts);

        var accounts = await _context.StorageAccounts
            .AsNoTracking()
            .Where(x => x.IsEnabled)
            .Select(x => new { x.CreatedBy, x.QuotaBytes, x.UsedBytes })
            .ToListAsync(cancellationToken);

        var queued = 0;
        foreach (var owner in accounts.GroupBy(x => x.CreatedBy))
        {
            var free = owner.Sum(x => Math.Max(0, x.QuotaBytes - x.UsedBytes));
            if (free < _options.MinFreeBytes) continue;

            var estimate = await EstimatedVideoBytesAsync(owner.Key, cancellationToken);
            var inFlight = await _context.Episodes.CountAsync(x => x.Work!.Type == WorkType.Movie && x.Work.CreatedBy == owner.Key
                && x.StreamUrl == null && x.QueuedAt != null && x.QueuedAt > requeueBefore && x.StorageAttempts < max, cancellationToken);
            // Số video kho còn chứa được, trừ các tập đang chờ trong hàng đợi.
            var room = (int)Math.Min(int.MaxValue, free / estimate) - inFlight;
            var take = Math.Min(Math.Max(1, _options.BatchSize), room);
            if (take <= 0) continue;

            var episodes = await _context.Episodes
                .Where(x => x.Work!.Type == WorkType.Movie && x.Work.CreatedBy == owner.Key
                            && x.StreamUrl == null && x.VideoUrl != null
                            && (x.QueuedAt == null || x.QueuedAt <= requeueBefore)
                            && (x.StorageAttempts < max
                                || (exhaustedBefore != null && (x.QueuedAt == null || x.QueuedAt <= exhaustedBefore))))
                // Tập chưa thử lần nào đi trước để một tập lỗi không chặn cả hàng.
                .OrderBy(x => x.StorageAttempts).ThenBy(x => x.WorkId).ThenBy(x => x.EpisodeNumber)
                .Take(take)
                .ToListAsync(cancellationToken);

            foreach (var episode in episodes)
            {
                // Tập đã lỗi hết số lần: cho thử thêm đúng một lần.
                if (episode.StorageAttempts >= max) episode.StorageAttempts = max - 1;
                episode.QueuedAt = now;
                episode.AddIntegrationEvent(new EpisodeVideoStoreRequestedIntegrationEvent { EpisodeId = episode.Id }, cancellationToken);
            }

            queued += episodes.Count;
        }

        if (queued > 0) await _context.SaveChangesAsync(cancellationToken);
        return queued;
    }

    /// <summary>Dung lượng trung bình một video đã lưu của người dùng; chưa có thì dùng ước tính trong cấu hình.</summary>
    private async Task<long> EstimatedVideoBytesAsync(string? owner, CancellationToken cancellationToken)
    {
        var average = await _context.Episodes
            .Where(x => x.Work!.Type == WorkType.Movie && x.Work.CreatedBy == owner && x.StorageSizeBytes != null)
            .Select(x => (double?)x.StorageSizeBytes)
            .AverageAsync(cancellationToken);

        return Math.Max(1, (long)(average ?? _options.EstimatedVideoBytes));
    }

    /// <summary>
    /// Xử lý một tập lấy từ hàng đợi: bỏ qua nếu đã có link phát, hết lượt thử hoặc kho không còn chỗ; còn lại tải và lưu.
    /// </summary>
    /// <exception cref="TikTokBlockedException">TikTok đang chặn máy chủ / chưa đăng nhập: bên gọi nên nghỉ.</exception>
    public async Task StoreQueuedAsync(int episodeId, CancellationToken cancellationToken)
    {
        var episode = await _context.Episodes
            .Include(x => x.Work)
            .FirstOrDefaultAsync(x => x.Id == episodeId, cancellationToken);

        if (episode?.Work is not { Type: WorkType.Movie } work) return;
        if (episode.StreamUrl is not null || string.IsNullOrWhiteSpace(episode.VideoUrl)) return;
        if (episode.StorageAttempts >= _options.MaxAttempts) return;

        // Hàng đợi có thể đã chờ lâu: kho có thể đã đầy từ lúc xếp hàng. Không tải khi không có chỗ để.
        var free = await _context.StorageAccounts
            .Where(x => x.CreatedBy == work.CreatedBy && x.IsEnabled)
            .Select(x => x.QuotaBytes - x.UsedBytes)
            .ToListAsync(cancellationToken);
        if (free.Sum(x => Math.Max(0, x)) < _options.MinFreeBytes)
        {
            episode.StorageError = "Kho lưu trữ không còn đủ chỗ trống — thêm tài khoản mới hoặc nâng hạn mức, hệ thống sẽ tự tải lại.";
            episode.QueuedAt = null;
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        await StoreAsync(episode, cancellationToken);
    }

    /// <summary>Tải và lưu video của một tập; lỗi được ghi vào tập (<see cref="Episode.StorageError"/>) chứ không ném ra.</summary>
    /// <exception cref="TikTokBlockedException">TikTok đang chặn máy chủ (đã ghi vào tập, không tính là một lần thử).</exception>
    public async Task StoreAsync(Episode episode, CancellationToken cancellationToken)
    {
        var temp = Path.Combine(Path.GetTempPath(), $"episode-{episode.Id}-{Guid.NewGuid():N}.mp4");
        try
        {
            var size = await DownloadAsync(episode, temp, cancellationToken);
            // App phát ngay khi tải được đoạn đầu file, nên mục lục của video phải nằm ở đầu.
            if (Mp4FastStart.Apply(temp)) _logger.LogInformation("Tập {EpisodeId}: đã đưa mục lục video lên đầu file", episode.Id);
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
        catch (StorageFullException ex)
        {
            // Hết chỗ không phải lỗi của tập: không tính lần thử, cho xếp hàng lại khi kho có thêm dung lượng.
            _logger.LogWarning("Tập {EpisodeId}: kho đầy: {Error}", episode.Id, ex.Message);
            episode.StorageKey = null;
            episode.StorageError = ex.Message.Length <= MaxErrorLength ? ex.Message : ex.Message[..MaxErrorLength];
            episode.QueuedAt = null;
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
    /// Tải file video của tập về <paramref name="filePath"/>. Tập của drama: lấy link file từ API danh sách tập của TikTok
    /// bằng cookie tài khoản (TikTok chỉ trả link cho phiên đã đăng nhập — khảo sát 2026-10-11) rồi tải bằng chính cookie
    /// đó. Tập không thuộc drama: mở trang video.
    /// </summary>
    private async Task<long> DownloadAsync(Episode episode, string filePath, CancellationToken cancellationToken)
    {
        var owner = episode.Work!.CreatedBy;
        var cookie = await TikTokCookieAsync(owner, cancellationToken);
        var dramaId = episode.Work.ExternalId;

        if (string.IsNullOrEmpty(dramaId) || string.IsNullOrEmpty(episode.ExternalId))
            return await _tikTok.DownloadVideoAsync(cookie, episode.VideoUrl!, filePath, _options.MaxVideoBytes, cancellationToken);

        if (string.IsNullOrEmpty(cookie))
        {
            throw new TikTokBlockedException(
                "Chưa có tài khoản TikTok nào đang đăng nhập — TikTok chỉ cấp file tập drama cho phiên đã đăng nhập. Vào mục Tài khoản, bấm “Mở Chrome” và đăng nhập TikTok.");
        }

        var files = await DramaFilesAsync(owner, cookie, dramaId, cancellationToken);
        if (!files.TryGetValue(episode.ExternalId, out var urls) || urls.Count == 0)
        {
            // Danh sách đã lưu có thể cũ (tập mới thêm sau): đọc lại một lần trước khi kết luận tập không có file.
            _dramaFiles.Remove(owner, dramaId);
            files = await DramaFilesAsync(owner, cookie, dramaId, cancellationToken);
            if (files.TryGetValue(episode.ExternalId, out urls) && urls.Count > 0)
                return await _tikTok.DownloadVideoFileAsync(cookie, urls, filePath, _options.MaxVideoBytes, cancellationToken);
        }

        if (urls is null || urls.Count == 0)
            throw new TikTokVideoUnavailableException("TikTok không cấp link tải cho tập này (tập đã bị gỡ hoặc tài khoản không xem được).");

        return await _tikTok.DownloadVideoFileAsync(cookie, urls, filePath, _options.MaxVideoBytes, cancellationToken);
    }

    /// <summary>Link file của mọi tập trong một drama (id video → các link), đọc một lần cho mỗi lượt.</summary>
    /// <exception cref="TikTokBlockedException">TikTok không trả link file cho tập nào: cookie đã hết hạn/bị đăng xuất.</exception>
    private async Task<Dictionary<string, List<string>>> DramaFilesAsync(string? owner, string cookie, string dramaId,
        CancellationToken cancellationToken)
    {
        if (_dramaFiles.TryGet(owner, dramaId) is { } cached) return cached;

        var files = new Dictionary<string, List<string>>();
        var cursor = "0";
        for (var page = 0; page < MaxEpisodePages; page++)
        {
            var result = await _tikTok.FetchDramaEpisodes(cookie, dramaId, EpisodePageSize, cursor, cancellationToken);
            var added = 0;
            foreach (var item in result.ItemList.Where(x => x.Id is not null))
            {
                if (files.TryAdd(item.Id!, item.Video?.DownloadUrls() ?? [])) added++;
            }

            if (!result.HasMore || added == 0 || string.IsNullOrEmpty(result.Cursor) || result.Cursor == cursor) break;
            cursor = result.Cursor;
        }

        if (files.Count > 0 && files.Values.All(x => x.Count == 0))
        {
            throw new TikTokBlockedException(
                "TikTok không cấp link file cho tài khoản đang lưu (cookie đã hết hạn hoặc bị đăng xuất). Vào mục Tài khoản, bấm “Mở Chrome” và đăng nhập TikTok lại.");
        }

        _dramaFiles.Set(owner, dramaId, files);
        return files;
    }

    /// <summary>
    /// Cookie của tài khoản TikTok mà chủ phim đăng nhập gần nhất (loại tài khoản có trang đăng nhập thuộc TikTok);
    /// <c>null</c> khi chưa có.
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
            throw new StorageFullException(
                $"Không tài khoản lưu trữ nào còn đủ {size / (double)StorageAccount.BytesPerMb:0.#} MB trống. Thêm tài khoản mới hoặc nâng hạn mức rồi bấm thử lại.");
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
