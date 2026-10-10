using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Works.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.Works.Queries.GetWorks;

public record GetWorksQuery : IRequest<PaginatedList<WorkDto>>
{
    public string? Title { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetWorksQueryValidator : AbstractValidator<GetWorksQuery>
{
    public GetWorksQueryValidator()
    {
    }
}

public class GetWorksQueryHandler : IRequestHandler<GetWorksQuery, PaginatedList<WorkDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public GetWorksQueryHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public async Task<PaginatedList<WorkDto>> Handle(GetWorksQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Works
            .Where(x=> x.CreatedBy == _user.Id);
        
        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => x.Title != null && x.Title.Contains(request.Title));

        return await PaginatedList<WorkDto>.CreateAsync(
            query.OrderByDescending(x => x.Created).ProjectTo<WorkDto>(_mapper.ConfigurationProvider),
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
