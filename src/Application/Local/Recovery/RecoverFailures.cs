using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.AIs;
using SurveillanceCameras.Application.Local.CrawlStories;
using SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;
using SurveillanceCameras.Application.Local.Tyt;
using SurveillanceCameras.Application.Local.Works.IntegrationEvents;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.Tyt;
using SurveillanceCameras.Domain.Constants;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Recovery;

/// <summary>Kết quả xử lý lỗi của một nhóm tác vụ dùng chung một phụ thuộc (một nguồn web, một tài khoản TYT, một bộ AI).</summary>
/// <param name="Group">Tên nhóm, vd. "Tải chương · nguồn web 3".</param>
/// <param name="Healthy">Phụ thuộc dùng được (hết bị chặn, cookie còn hạn, phiên TYT còn đăng nhập, AI trả lời).</param>
/// <param name="Reason">Lý do chưa dùng được; <c>null</c> khi <paramref name="Healthy"/>.</param>
/// <param name="Failed">Số tác vụ lỗi đang chờ xử lý lại.</param>
/// <param name="Requeued">Số tác vụ được đưa lại vào hàng đợi lượt này (0 khi chưa khắc phục được).</param>
public record RecoveryGroupResult(string Group, bool Healthy, string? Reason, int Failed, int Requeued);

/// <summary>
/// Chạy định kỳ: xử lý lại các tác vụ đã lỗi — nhưng chỉ khi nguyên nhân thường gặp đã được khắc phục. Mỗi nhóm tác vụ
/// được kiểm tra phụ thuộc của nó trước bằng một request thử (IP bị chặn, cookie hết hạn, phiên TYT hết đăng nhập, AI hết
/// quota…). Chưa khắc phục thì bỏ qua cả nhóm, chờ chu kỳ kiểm tra kế tiếp, không dồn thêm request vào nơi đang lỗi.
/// Các nhóm độc lập: nguồn A bị chặn không làm nguồn B hay TYT ngừng theo.
/// </summary>
/// <param name="MaxPerGroup">Số tác vụ tối đa đưa lại vào hàng đợi cho mỗi nhóm trong một lượt.</param>
/// <param name="RequeueAfter">Tác vụ vừa được đưa vào hàng đợi chưa quá khoảng này thì chưa đưa lại (hàng đợi còn xử lý).</param>
public record RecoverFailuresCommand(int MaxPerGroup = 50, TimeSpan? RequeueAfter = null) : IRequest<IReadOnlyList<RecoveryGroupResult>>;

public class RecoverFailuresCommandHandler : IRequestHandler<RecoverFailuresCommand, IReadOnlyList<RecoveryGroupResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICrawlStoryRepositoryFactory _crawlFactory;
    private readonly AIPromptRunner _aiRunner;
    private readonly ITytRepository _tyt;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RecoverFailuresCommandHandler> _logger;

    public RecoverFailuresCommandHandler(IApplicationDbContext context, ICrawlStoryRepositoryFactory crawlFactory,
        AIPromptRunner aiRunner, ITytRepository tyt, TimeProvider timeProvider, ILogger<RecoverFailuresCommandHandler> logger)
    {
        _context = context;
        _crawlFactory = crawlFactory;
        _aiRunner = aiRunner;
        _tyt = tyt;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private sealed record Item(int Id, string? Owner, int Key);

    public async Task<IReadOnlyList<RecoveryGroupResult>> Handle(RecoverFailuresCommand request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var cutoff = now - (request.RequeueAfter ?? TimeSpan.FromMinutes(60));
        var results = new List<RecoveryGroupResult>();

        await RecoverFetchAsync(request, cutoff, now, results, cancellationToken);
        await RecoverEditAsync(request, cutoff, now, results, cancellationToken);
        await RecoverTytStoriesAsync(request, cutoff, now, results, cancellationToken);
        await RecoverTytChaptersAsync(request, cutoff, now, results, cancellationToken);

        foreach (var r in results)
        {
            if (r.Healthy) _logger.LogInformation("Khôi phục lỗi · {Group}: {Failed} lỗi, đưa lại {Requeued} vào hàng đợi", r.Group, r.Failed, r.Requeued);
            else _logger.LogWarning("Khôi phục lỗi · {Group}: {Failed} lỗi, chưa khắc phục ({Reason}) — chờ chu kỳ sau", r.Group, r.Failed, r.Reason);
        }

        return results;
    }

    // ── Tải nội dung chương: phụ thuộc vào nguồn web (IP/cookie) ───────────────────────────────────────────────────────
    private async Task RecoverFetchAsync(RecoverFailuresCommand request, DateTimeOffset cutoff, DateTimeOffset now,
        List<RecoveryGroupResult> results, CancellationToken ct)
    {
        var failed = await _context.Episodes
            .AsNoTracking()
            .Where(x => x.Status == EpisodeStatus.FetchFailed && x.Work!.Type == WorkType.Story && x.Work.StorySource != null)
            .Select(x => new { x.Id, x.QueuedAt, Owner = x.Work!.CreatedBy, WebSourceId = x.Work.StorySource!.WebSourceId })
            .ToListAsync(ct);

        foreach (var group in failed.GroupBy(x => (x.Owner, x.WebSourceId)))
        {
            var name = $"Tải chương · nguồn web {group.Key.WebSourceId}";
            var due = group.Where(x => x.QueuedAt is null || x.QueuedAt <= cutoff).Select(x => x.Id).ToList();
            if (due.Count == 0) { results.Add(new(name, true, null, group.Count(), 0)); continue; }

            var problem = await ProbeCrawlAsync(group.Key.Owner, group.Key.WebSourceId, ct);
            if (problem is not null) { results.Add(new(name, false, problem, group.Count(), 0)); continue; }

            var requeued = await RequeueEpisodesAsync(due, request.MaxPerGroup, now,
                id => new ChapterFetchRequestedIntegrationEvent { ChapterId = id }, ct);
            results.Add(new(name, true, null, group.Count(), requeued));
        }
    }

    private async Task<string?> ProbeCrawlAsync(string? owner, int webSourceId, CancellationToken ct)
    {
        try
        {
            var source = await CrawlSource.LoadAsync(_context, owner, _crawlFactory, webSourceId, ct);
            // Một request thật tới trang nguồn: thấy bị chặn IP, hết cookie hay lỗi mạng ngay tại đây thay vì trên cả nghìn chương.
            await source.Repository.FetchStoryListAsync(source.Context, 1, ct);
            return null;
        }
        catch (CrawlBlockedException ex) { return $"IP đang bị chặn: {ex.Message}"; }
        catch (CrawlException ex) { return ex.Message; }
        catch (NotFoundException) { return "Nguồn web đã bị xoá."; }
    }

    // ── Biên tập AI: phụ thuộc vào prompt + API key + quota ────────────────────────────────────────────────────────────
    private async Task RecoverEditAsync(RecoverFailuresCommand request, DateTimeOffset cutoff, DateTimeOffset now,
        List<RecoveryGroupResult> results, CancellationToken ct)
    {
        var failed = await _context.Episodes
            .AsNoTracking()
            .Where(x => x.Status == EpisodeStatus.EditFailed && x.Work!.Type == WorkType.Story)
            .Select(x => new { x.Id, x.QueuedAt, Owner = x.Work!.CreatedBy })
            .ToListAsync(ct);

        foreach (var group in failed.GroupBy(x => x.Owner))
        {
            var name = $"Biên tập AI · người dùng {group.Key}";
            var due = group.Where(x => x.QueuedAt is null || x.QueuedAt <= cutoff).Select(x => x.Id).ToList();
            if (due.Count == 0) { results.Add(new(name, true, null, group.Count(), 0)); continue; }

            var problem = await ProbeAiAsync(group.Key, ct);
            if (problem is not null) { results.Add(new(name, false, problem, group.Count(), 0)); continue; }

            var requeued = await RequeueEpisodesAsync(due, request.MaxPerGroup, now,
                id => new ChapterEditRequestedIntegrationEvent { ChapterId = id }, ct);
            results.Add(new(name, true, null, group.Count(), requeued));
        }
    }

    private async Task<string?> ProbeAiAsync(string? owner, CancellationToken ct)
    {
        var prompt = await _aiRunner.LatestPromptAsync(owner, PromptTypeCodes.EditChapterContent, ct);
        if (prompt is null) return $"Chưa có prompt loại {PromptTypeCodes.EditChapterContent}.";

        var ais = await _aiRunner.AvailableAIsAsync(owner, ct);
        try
        {
            // Một lời gọi ngắn để biết key còn quota và model còn trả lời, trước khi gửi lại cả loạt chương.
            await _aiRunner.RunAsync(prompt, ais, "Chỉ trả lời đúng một từ: OK.", "Kiểm tra kết nối.", ct);
            return null;
        }
        catch (AIProviderException ex) { return ex.Message; }
    }

    // ── Đưa truyện lên TYT: phụ thuộc vào phiên đăng nhập của tài khoản ────────────────────────────────────────────────
    private async Task RecoverTytStoriesAsync(RecoverFailuresCommand request, DateTimeOffset cutoff, DateTimeOffset now,
        List<RecoveryGroupResult> results, CancellationToken ct)
    {
        var failed = await _context.Works
            .AsNoTracking()
            .Where(x => x.Type == WorkType.Story && x.TytError != null && x.AccountId != null)
            .Select(x => new { x.Id, x.CreatedBy, AccountId = x.AccountId!.Value, x.TytRetryQueuedAt })
            .ToListAsync(ct);

        foreach (var group in failed.GroupBy(x => (x.CreatedBy, x.AccountId)))
        {
            var name = $"Đưa truyện lên TYT · tài khoản {group.Key.AccountId}";
            var due = group.Where(x => x.TytRetryQueuedAt is null || x.TytRetryQueuedAt <= cutoff).Select(x => x.Id).ToList();
            if (due.Count == 0) { results.Add(new(name, true, null, group.Count(), 0)); continue; }

            var problem = await ProbeTytAsync(group.Key.CreatedBy, group.Key.AccountId, ct);
            if (problem is not null) { results.Add(new(name, false, problem, group.Count(), 0)); continue; }

            var works = await _context.Works.Where(x => due.Take(request.MaxPerGroup).Contains(x.Id)).ToListAsync(ct);
            foreach (var work in works)
            {
                work.TytError = null;
                work.TytRetryQueuedAt = now;
                work.AddIntegrationEvent(new TytStoryCreateRequestedIntegrationEvent { StoryId = work.Id }, ct);
            }

            await _context.SaveChangesAsync(ct);
            results.Add(new(name, true, null, group.Count(), works.Count));
        }
    }

    // ── Đăng chương lên TYT ───────────────────────────────────────────────────────────────────────────────────────────
    private async Task RecoverTytChaptersAsync(RecoverFailuresCommand request, DateTimeOffset cutoff, DateTimeOffset now,
        List<RecoveryGroupResult> results, CancellationToken ct)
    {
        var failed = await _context.Episodes
            .AsNoTracking()
            .Where(x => !x.IsUploaded && x.TytUploadError != null && x.Work!.Type == WorkType.Story && x.Work.AccountId != null)
            .Select(x => new { x.Id, x.QueuedAt, Owner = x.Work!.CreatedBy, AccountId = x.Work.AccountId!.Value })
            .ToListAsync(ct);

        foreach (var group in failed.GroupBy(x => (x.Owner, x.AccountId)))
        {
            var name = $"Đăng chương lên TYT · tài khoản {group.Key.AccountId}";
            var due = group.Where(x => x.QueuedAt is null || x.QueuedAt <= cutoff).Select(x => x.Id).ToList();
            if (due.Count == 0) { results.Add(new(name, true, null, group.Count(), 0)); continue; }

            var problem = await ProbeTytAsync(group.Key.Owner, group.Key.AccountId, ct);
            if (problem is not null) { results.Add(new(name, false, problem, group.Count(), 0)); continue; }

            var requeued = await RequeueEpisodesAsync(due, request.MaxPerGroup, now,
                id => new ChapterTytUploadRequestedIntegrationEvent { ChapterId = id }, ct);
            results.Add(new(name, true, null, group.Count(), requeued));
        }
    }

    private async Task<string?> ProbeTytAsync(string? owner, int accountId, CancellationToken ct)
    {
        try
        {
            var context = await TytAccounts.ContextAsync(_context, accountId, owner, ct);
            await _tyt.CheckSessionAsync(context, ct);
            return null;
        }
        catch (TytException ex) { return ex.Message; }
    }

    /// <summary>Đưa các chương (tối đa <paramref name="max"/>, số chương nhỏ trước) vào hàng đợi bằng sự kiện do <paramref name="createEvent"/> tạo.</summary>
    private async Task<int> RequeueEpisodesAsync(List<int> ids, int max, DateTimeOffset now,
        Func<int, IntegrationEvent> createEvent, CancellationToken ct)
    {
        var take = ids.Take(Math.Max(1, max)).ToList();
        var chapters = await _context.Episodes.Where(x => take.Contains(x.Id)).OrderBy(x => x.EpisodeNumber).ToListAsync(ct);
        foreach (var chapter in chapters)
        {
            chapter.QueuedAt = now;
            chapter.AddIntegrationEvent(createEvent(chapter.Id), ct);
        }

        await _context.SaveChangesAsync(ct);
        return chapters.Count;
    }
}
