using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;
using SurveillanceCameras.Application.Repository.VideoStorage;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.StorageAccounts.Commands.TrimStorageAccount;

/// <summary>
/// Đưa dung lượng thực của bucket về dưới hạn mức của tài khoản lưu trữ. Dung lượng đã dùng được đặt lại theo bucket, rồi
/// nếu còn vượt hạn mức thì gỡ video của các tập cho tới khi vừa: phim ít lượt xem trước, trong một phim thì tập cuối
/// trước (giữ lại phim nhiều người xem và các tập đầu). Tập bị gỡ quay về trạng thái chưa có link phát.
/// </summary>
/// <param name="Apply"><c>false</c>: chỉ tính và trả về kế hoạch, không xoá gì.</param>
/// <param name="DeleteOrphans">
/// Xoá cả file "mồ côi" — file trong thư mục <c>movies/</c> của bucket mà không tập nào của hệ thống này trỏ tới. Chỉ bật
/// khi bucket không dùng chung với hệ thống/máy chủ khác, vì file của bên đó cũng sẽ bị coi là mồ côi.
/// </param>
public record TrimStorageAccountCommand(int Id, bool Apply, bool DeleteOrphans) : IRequest<TrimStorageAccountResult>;

/// <param name="UsedBytesBefore">Dung lượng thực trong bucket trước khi dọn.</param>
/// <param name="UsedBytesAfter">Dung lượng sau khi dọn (dự kiến, nếu chưa áp dụng).</param>
/// <param name="OrphanObjects">Số file mồ côi tìm thấy và dung lượng của chúng (<paramref name="OrphanBytes"/>).</param>
/// <param name="EpisodesReleased">Số tập bị gỡ video (sẽ bị gỡ, nếu chưa áp dụng).</param>
/// <param name="Movies">Tóm tắt theo phim: tên phim và các tập bị gỡ.</param>
/// <param name="Errors">File không xoá được (dung lượng của chúng vẫn bị tính).</param>
public record TrimStorageAccountResult(bool Applied, long QuotaBytes, long UsedBytesBefore, long UsedBytesAfter, int Objects,
    int OrphanObjects, long OrphanBytes, bool OrphansDeleted, int EpisodesReleased, long EpisodeBytesReleased,
    List<TrimmedMovie> Movies, List<string> Errors)
{
    public bool WithinQuota => UsedBytesAfter <= QuotaBytes;
}

public record TrimmedMovie(int MovieId, string? Title, int Episodes, int FromEpisode, int ToEpisode, long Bytes);

public class TrimStorageAccountCommandHandler : IRequestHandler<TrimStorageAccountCommand, TrimStorageAccountResult>
{
    /// <summary>Thư mục trong bucket mà hệ thống lưu video tập phim — chỉ file ở đây mới được coi là mồ côi.</summary>
    public const string ManagedPrefix = "movies/";

    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ISecretProtector _protector;
    private readonly IVideoStorageRepository _storage;
    private readonly EpisodeVideoStorer _storer;
    private readonly TimeProvider _timeProvider;
    private readonly EpisodeVideoStoreOptions _options;

    public TrimStorageAccountCommandHandler(IApplicationDbContext context, IUser user, ISecretProtector protector,
        IVideoStorageRepository storage, EpisodeVideoStorer storer, TimeProvider timeProvider,
        IOptions<EpisodeVideoStoreOptions> options)
    {
        _options = options.Value;
        _context = context;
        _user = user;
        _protector = protector;
        _storage = storage;
        _storer = storer;
        _timeProvider = timeProvider;
    }

    /// <exception cref="VideoStorageException">Không đọc được bucket (chưa xoá gì).</exception>
    public async Task<TrimStorageAccountResult> Handle(TrimStorageAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.StorageAccounts
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);
        Guard.Against.NotFound(request.Id, account);

        var target = account.ToTarget(_protector);
        var objects = await _storage.ListAsync(target, cancellationToken);
        var usedBefore = objects.Sum(x => x.Size);
        var sizes = objects.GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.First().Size);

        var stored = await _context.Episodes
            .Include(x => x.Work)
            .Where(x => x.StorageAccountId == account.Id && x.StorageKey != null)
            .ToListAsync(cancellationToken);
        var referenced = stored.Select(x => x.StorageKey!).ToHashSet();

        var orphans = objects.Where(x => x.Key.StartsWith(ManagedPrefix, StringComparison.Ordinal) && !referenced.Contains(x.Key)).ToList();
        var orphanBytes = orphans.Sum(x => x.Size);
        var used = usedBefore - (request.DeleteOrphans ? orphanBytes : 0);

        // Gỡ cho tới khi vừa hạn mức. Kích thước lấy theo file thực trong bucket (file đã mất thì không giải phóng được gì).
        var release = new List<(Episode Episode, long Size)>();
        foreach (var episode in stored
                     .OrderBy(x => x.Work!.ViewCount ?? 0).ThenBy(x => x.WorkId)
                     .ThenByDescending(x => x.EpisodeNumber).ThenByDescending(x => x.Id))
        {
            if (used <= account.QuotaBytes) break;
            var size = sizes.GetValueOrDefault(episode.StorageKey!, 0);
            release.Add((episode, size));
            used -= size;
        }

        var movies = release
            .GroupBy(x => x.Episode.WorkId)
            .Select(g => new TrimmedMovie(g.Key, g.First().Episode.Work!.Title, g.Count(),
                g.Min(x => x.Episode.EpisodeNumber), g.Max(x => x.Episode.EpisodeNumber), g.Sum(x => x.Size)))
            .ToList();
        var errors = new List<string>();

        if (request.Apply)
        {
            if (request.DeleteOrphans)
            {
                foreach (var orphan in orphans)
                {
                    try
                    {
                        await _storage.DeleteAsync(target, orphan.Key, cancellationToken);
                    }
                    catch (VideoStorageException ex)
                    {
                        errors.Add($"{orphan.Key}: {ex.Message}");
                    }
                }
            }

            // Xoá file của tập và gỡ link phát (ReleaseAsync tự ghi log file không xoá được).
            await _storer.ReleaseAsync(release.Select(x => x.Episode).ToList(), cancellationToken);
            foreach (var (episode, _) in release)
            {
                // Không để tác vụ nền tải lại ngay các tập vừa gỡ; người dùng bấm "Lưu video lại" khi đã có chỗ.
                episode.StorageAttempts = _options.MaxAttempts;
                episode.StorageError = "Đã gỡ video khỏi kho để đưa dung lượng về dưới hạn mức.";
            }

            // Đối soát lại theo bucket sau khi xoá: con số cuối cùng là dung lượng thực, kể cả file xoá lỗi.
            used = (await _storage.ListAsync(target, cancellationToken)).Sum(x => x.Size);
            account.UsedBytes = used;
            account.LastError = null;
            account.LastCheckedAt = _timeProvider.GetUtcNow();
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new TrimStorageAccountResult(request.Apply, account.QuotaBytes, usedBefore, used, objects.Count,
            orphans.Count, orphanBytes, request.DeleteOrphans, release.Count, release.Sum(x => x.Size), movies, errors);
    }
}
