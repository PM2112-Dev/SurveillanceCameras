using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Episodes.Queries.GetWorkEpisodeStats;

/// <summary>Tiến độ lấy và biên tập chương của truyện (đếm theo trạng thái, không tải nội dung).</summary>
public record GetWorkEpisodeStatsQuery(int WorkId) : IRequest<WorkEpisodeStatsDto>;

public record WorkEpisodeStatsDto
{
    public int Total { get; init; }
    public int Listed { get; init; }
    public int Fetched { get; init; }
    public int Edited { get; init; }
    public int FetchFailed { get; init; }
    public int EditFailed { get; init; }

    /// <summary>Chương đã tải nhưng chưa biên tập vì chưa có prompt (không phải đang chờ AI).</summary>
    public int WaitingForPrompt { get; init; }

    public DateTimeOffset? EpisodesSyncedAt { get; init; }
    public string? EpisodeSyncError { get; init; }
}

public class GetWorkEpisodeStatsQueryHandler : IRequestHandler<GetWorkEpisodeStatsQuery, WorkEpisodeStatsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetWorkEpisodeStatsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<WorkEpisodeStatsDto> Handle(GetWorkEpisodeStatsQuery request, CancellationToken cancellationToken)
    {
        var story = await _context.Works
            .AsNoTracking()
            .Where(x => x.Id == request.WorkId && x.CreatedBy == _user.Id)
            .Select(x => new { x.EpisodesSyncedAt, x.EpisodeSyncError })
            .FirstOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(request.WorkId, story);

        var counts = await _context.Episodes
            .Where(x => x.WorkId == request.WorkId)
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        var waitingForPrompt = await _context.Episodes
            .CountAsync(x => x.WorkId == request.WorkId && x.Status == EpisodeStatus.Fetched && x.Error != null,
                cancellationToken);

        int Count(EpisodeStatus status) => counts.GetValueOrDefault(status);

        return new WorkEpisodeStatsDto
        {
            Total = counts.Values.Sum(),
            Listed = Count(EpisodeStatus.Listed),
            Fetched = Count(EpisodeStatus.Fetched),
            Edited = Count(EpisodeStatus.Edited),
            FetchFailed = Count(EpisodeStatus.FetchFailed),
            EditFailed = Count(EpisodeStatus.EditFailed),
            WaitingForPrompt = waitingForPrompt,
            EpisodesSyncedAt = story.EpisodesSyncedAt,
            EpisodeSyncError = story.EpisodeSyncError
        };
    }
}
