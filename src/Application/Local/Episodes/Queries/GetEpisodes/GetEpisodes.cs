using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Episodes.Queries.DTOs;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Episodes.Queries.GetEpisodes;

/// <summary>Danh sách chương của một truyện theo thứ tự chương (không kèm nội dung).</summary>
public record GetEpisodesQuery : IRequest<PaginatedList<EpisodeBriefDto>>
{
    public int WorkId { get; init; }
    public string? Title { get; init; }
    public EpisodeStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public class GetEpisodesQueryValidator : AbstractValidator<GetEpisodesQuery>
{
    public GetEpisodesQueryValidator()
    {
        RuleFor(x => x.WorkId).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public class GetEpisodesQueryHandler : IRequestHandler<GetEpisodesQuery, PaginatedList<EpisodeBriefDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetEpisodesQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<EpisodeBriefDto>> Handle(GetEpisodesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Episodes.Where(x => x.WorkId == request.WorkId && x.CreatedBy == _user.Id && x.Work!.Type == WorkType.Story);

        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => (x.NameRaw != null && x.NameRaw.Contains(request.Title))
                                     || (x.NameEdit != null && x.NameEdit.Contains(request.Title)));

        if (request.Status is { } status)
            query = query.Where(x => x.Status == status);

        return await PaginatedList<EpisodeBriefDto>.CreateAsync(
            query.OrderBy(x => x.EpisodeNumber).ThenBy(x => x.Id).ProjectTo<EpisodeBriefDto>(_mapper.ConfigurationProvider),
            request.Page, request.PageSize, cancellationToken);
    }
}
