using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Movies.Queries.DTOs;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Queries.GetMovieEpisodes;

/// <summary>Các tập của phim theo thứ tự tập.</summary>
public record GetMovieEpisodesQuery : IRequest<PaginatedList<MovieEpisodeDto>>
{
    public int MovieId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;

    /// <summary>Chỉ lấy tập có vấn đề: trùng số tập, lưu video lỗi hoặc không còn nguồn phát.</summary>
    public bool OnlyIssues { get; init; }

    /// <summary>Bộ lọc tập: <c>issues</c>, <c>stored</c>, <c>unstored</c>, <c>storageError</c>, <c>missingSource</c>, <c>duplicate</c>, <c>edited</c>.</summary>
    public string? Filter { get; init; }

    /// <summary>Tìm theo số tập, tên tập hoặc ID video.</summary>
    public string? Search { get; init; }
}

public class GetMovieEpisodesQueryValidator : AbstractValidator<GetMovieEpisodesQuery>
{
    public GetMovieEpisodesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public class GetMovieEpisodesQueryHandler : IRequestHandler<GetMovieEpisodesQuery, PaginatedList<MovieEpisodeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public GetMovieEpisodesQueryHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public async Task<PaginatedList<MovieEpisodeDto>> Handle(GetMovieEpisodesQuery request, CancellationToken cancellationToken)
    {
        var exists = await _context.Works.AnyAsync(
            x => x.Id == request.MovieId && x.CreatedBy == _user.Id && x.Type == WorkType.Movie, cancellationToken);
        if (!exists) throw new NotFoundException(request.MovieId.ToString(), "Movie");

        // Tập trùng số tập: tính trên cả phim (các bản sao có thể nằm ở trang khác).
        var duplicateNumbers = await _context.Episodes
            .Where(x => x.WorkId == request.MovieId)
            .GroupBy(x => x.EpisodeNumber)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);

        var query = _context.Episodes.Where(x => x.WorkId == request.MovieId);

        var filter = !string.IsNullOrWhiteSpace(request.Filter)
            ? request.Filter
            : (request.OnlyIssues ? "issues" : null);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = filter switch
            {
                "issues" => query.Where(x => duplicateNumbers.Contains(x.EpisodeNumber)
                                             || (x.StorageError != null && (x.StreamUrl == null || x.StreamUrl == ""))
                                             || ((x.VideoUrl == null || x.VideoUrl == "") && (x.StreamUrl == null || x.StreamUrl == ""))),
                "stored" => query.Where(x => x.StreamUrl != null && x.StreamUrl != ""),
                "unstored" => query.Where(x => x.StreamUrl == null || x.StreamUrl == ""),
                "storageError" => query.Where(x => x.StorageError != null && (x.StreamUrl == null || x.StreamUrl == "")),
                "missingSource" => query.Where(x => (x.VideoUrl == null || x.VideoUrl == "") && (x.StreamUrl == null || x.StreamUrl == "")),
                "duplicate" => query.Where(x => duplicateNumbers.Contains(x.EpisodeNumber)),
                "edited" => query.Where(x => x.IsEdited),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            if (int.TryParse(term, out var epNum))
            {
                query = query.Where(x => x.EpisodeNumber == epNum
                                         || (x.Title != null && x.Title.Contains(term))
                                         || (x.NameRaw != null && x.NameRaw.Contains(term))
                                         || (x.ExternalId != null && x.ExternalId.Contains(term)));
            }
            else
            {
                query = query.Where(x => (x.Title != null && x.Title.Contains(term))
                                         || (x.NameRaw != null && x.NameRaw.Contains(term))
                                         || (x.ExternalId != null && x.ExternalId.Contains(term)));
            }
        }

        var page = await PaginatedList<MovieEpisodeDto>.CreateAsync(
            query.OrderBy(x => x.EpisodeNumber).ThenBy(x => x.Id).ProjectTo<MovieEpisodeDto>(_mapper.ConfigurationProvider),
            request.Page, request.PageSize, cancellationToken);

        foreach (var item in page.Items)
        {
            item.IsDuplicate = duplicateNumbers.Contains(item.EpisodeNumber);
            item.IsMissingSource = string.IsNullOrWhiteSpace(item.VideoUrl) && string.IsNullOrWhiteSpace(item.StreamUrl);
        }

        return page;
    }
}
