using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Movies.Queries.DTOs;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Queries.GetMovies;

public enum MovieSort
{
    /// <summary>Mới đồng bộ trước.</summary>
    Newest = 0,

    /// <summary>Lượt xem từ cao đến thấp (phim chưa có lượt xem xếp cuối).</summary>
    MostViewed = 1,

    /// <summary>Lượt xem từ thấp đến cao.</summary>
    LeastViewed = 2,

    /// <summary>Số tập từ nhiều đến ít.</summary>
    MostEpisodes = 3,

    /// <summary>Số tập từ ít đến nhiều.</summary>
    LeastEpisodes = 4,

    /// <summary>Tổng thời lượng từ dài đến ngắn.</summary>
    LongestDuration = 5,

    /// <summary>Mới đồng bộ tập gần nhất.</summary>
    RecentlySynced = 6,

    /// <summary>Tên phim theo bảng chữ cái A → Z.</summary>
    TitleAsc = 7,

    /// <summary>Cũ nhất trước.</summary>
    Oldest = 8
}

public record GetMoviesQuery : IRequest<PaginatedList<MovieDto>>
{
    public string? Title { get; init; }

    /// <summary>Chỉ phim của kênh này; truyền <c>-1</c> để lọc phim chưa gắn kênh.</summary>
    public int? UserProfileId { get; init; }

    /// <summary>Chỉ phim thuộc ngôn ngữ này; truyền <c>-1</c> để lọc phim chưa gán ngôn ngữ.</summary>
    public int? LanguageId { get; init; }

    /// <summary>Lọc theo trạng thái hiển thị trên app (<c>true</c>: đang hiện, <c>false</c>: chưa xuất bản).</summary>
    public bool? IsPublished { get; init; }

    /// <summary>Trạng thái bộ tập: <c>full</c> (đủ tập), <c>ongoing</c> (đang ra), <c>empty</c> (0 tập).</summary>
    public string? Completion { get; init; }

    /// <summary>Quyền xem trên app: <c>vipOnly</c>, <c>nonVipOnly</c>, <c>custom</c>, <c>default</c>.</summary>
    public string? Access { get; init; }

    /// <summary>Trạng thái lưu trữ R2: <c>allStored</c>, <c>partialStored</c>, <c>noneStored</c>, <c>hasUnstored</c>, <c>storageError</c>.</summary>
    public string? Storage { get; init; }

    /// <summary>Tình trạng lỗi: <c>any</c>, <c>syncError</c>, <c>storageError</c>, <c>missingSource</c>, <c>duplicate</c>, <c>healthy</c>.</summary>
    public string? Issue { get; init; }

    public MovieSort Sort { get; init; } = MovieSort.Newest;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public class GetMoviesQueryValidator : AbstractValidator<GetMoviesQuery>
{
    public GetMoviesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.Sort).IsInEnum();
    }
}

public class GetMoviesQueryHandler : IRequestHandler<GetMoviesQuery, PaginatedList<MovieDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public GetMoviesQueryHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public Task<PaginatedList<MovieDto>> Handle(GetMoviesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Works.Where(x => x.CreatedBy == _user.Id && x.Type == WorkType.Movie);

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var term = request.Title.Trim();
            query = query.Where(x =>
                (x.Title != null && x.Title.Contains(term))
                || (x.ExternalId != null && x.ExternalId.Contains(term))
                || (x.UserProfile != null && ((x.UserProfile.Title != null && x.UserProfile.Title.Contains(term))
                                              || (x.UserProfile.UniqueId != null && x.UserProfile.UniqueId.Contains(term)))));
        }

        if (request.UserProfileId is { } userProfileId)
        {
            query = userProfileId == -1
                ? query.Where(x => x.UserProfileId == null)
                : query.Where(x => x.UserProfileId == userProfileId);
        }

        if (request.LanguageId is { } languageId)
        {
            query = languageId == -1
                ? query.Where(x => x.LanguageId == null)
                : query.Where(x => x.LanguageId == languageId);
        }

        if (request.IsPublished is { } isPublished)
            query = query.Where(x => x.IsPublished == isPublished);

        if (!string.IsNullOrWhiteSpace(request.Completion))
        {
            query = request.Completion switch
            {
                "full" => query.Where(x => x.IsFull),
                "ongoing" => query.Where(x => !x.IsFull && x.Episodes!.Any()),
                "empty" => query.Where(x => !x.Episodes!.Any()),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(request.Access))
        {
            query = request.Access switch
            {
                "vipOnly" => query.Where(x => x.IsVipOnly),
                "nonVipOnly" => query.Where(x => !x.IsVipOnly),
                "custom" => query.Where(x => x.HasCustomAccess),
                "default" => query.Where(x => !x.HasCustomAccess && !x.IsVipOnly),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(request.Storage))
        {
            query = request.Storage switch
            {
                "allStored" => query.Where(x => x.Episodes!.Any() && x.Episodes!.All(e => e.StreamUrl != null && e.StreamUrl != "")),
                "partialStored" => query.Where(x =>
                    x.Episodes!.Any(e => e.StreamUrl != null && e.StreamUrl != "")
                    && x.Episodes!.Any(e => e.StreamUrl == null || e.StreamUrl == "")),
                "noneStored" => query.Where(x => !x.Episodes!.Any(e => e.StreamUrl != null && e.StreamUrl != "")),
                "hasUnstored" => query.Where(x => x.Episodes!.Any(e => e.StreamUrl == null || e.StreamUrl == "")),
                "storageError" => query.Where(x => x.Episodes!.Any(e => e.StorageError != null && (e.StreamUrl == null || e.StreamUrl == ""))),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(request.Issue))
        {
            query = request.Issue switch
            {
                "syncError" => query.Where(x => x.EpisodeSyncError != null && x.EpisodeSyncError != ""),
                "storageError" => query.Where(x => x.Episodes!.Any(e => e.StorageError != null && (e.StreamUrl == null || e.StreamUrl == ""))),
                "missingSource" => query.Where(x => x.Episodes!.Any(e => (e.VideoUrl == null || e.VideoUrl == "") && (e.StreamUrl == null || e.StreamUrl == ""))),
                "duplicate" => query.Where(x => x.Episodes!.GroupBy(e => e.EpisodeNumber).Any(g => g.Count() > 1)),
                "any" => query.Where(x =>
                    (x.EpisodeSyncError != null && x.EpisodeSyncError != "")
                    || x.Episodes!.Any(e => e.StorageError != null && (e.StreamUrl == null || e.StreamUrl == ""))
                    || x.Episodes!.Any(e => (e.VideoUrl == null || e.VideoUrl == "") && (e.StreamUrl == null || e.StreamUrl == ""))
                    || x.Episodes!.GroupBy(e => e.EpisodeNumber).Any(g => g.Count() > 1)),
                "healthy" => query.Where(x =>
                    (x.EpisodeSyncError == null || x.EpisodeSyncError == "")
                    && !x.Episodes!.Any(e => e.StorageError != null && (e.StreamUrl == null || e.StreamUrl == ""))
                    && !x.Episodes!.Any(e => (e.VideoUrl == null || e.VideoUrl == "") && (e.StreamUrl == null || e.StreamUrl == ""))
                    && !x.Episodes!.GroupBy(e => e.EpisodeNumber).Any(g => g.Count() > 1)),
                _ => query
            };
        }

        return PaginatedList<MovieDto>.CreateAsync(
            Sorted(query, request.Sort).ProjectTo<MovieDto>(_mapper.ConfigurationProvider),
            request.Page, request.PageSize, cancellationToken);
    }

    private static IQueryable<Work> Sorted(IQueryable<Work> query, MovieSort sort)
        => sort switch
        {
            MovieSort.MostViewed => query.OrderBy(x => x.ViewCount == null).ThenByDescending(x => x.ViewCount).ThenByDescending(x => x.Id),
            MovieSort.LeastViewed => query.OrderBy(x => x.ViewCount == null).ThenBy(x => x.ViewCount).ThenByDescending(x => x.Id),
            MovieSort.MostEpisodes => query.OrderByDescending(x => x.Episodes!.Count).ThenByDescending(x => x.Id),
            MovieSort.LeastEpisodes => query.OrderBy(x => x.Episodes!.Count).ThenByDescending(x => x.Id),
            MovieSort.LongestDuration => query.OrderByDescending(x => x.Episodes!.Sum(e => e.DurationSeconds ?? 0)).ThenByDescending(x => x.Id),
            MovieSort.RecentlySynced => query.OrderBy(x => x.EpisodesSyncedAt == null).ThenByDescending(x => x.EpisodesSyncedAt).ThenByDescending(x => x.Id),
            MovieSort.TitleAsc => query.OrderBy(x => x.Title == null || x.Title == "").ThenBy(x => x.Title).ThenByDescending(x => x.Id),
            MovieSort.Oldest => query.OrderBy(x => x.Created).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.Created).ThenByDescending(x => x.Id)
        };
}
