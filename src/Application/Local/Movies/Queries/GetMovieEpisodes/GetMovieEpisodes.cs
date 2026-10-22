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
        if (request.OnlyIssues)
        {
            query = query.Where(x => duplicateNumbers.Contains(x.EpisodeNumber)
                                     || (x.StorageError != null && x.StreamUrl == null)
                                     || ((x.VideoUrl == null || x.VideoUrl == "") && (x.StreamUrl == null || x.StreamUrl == "")));
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
