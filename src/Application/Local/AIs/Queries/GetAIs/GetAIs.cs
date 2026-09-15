using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.AIs.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.AIs.Queries.GetAIs;

public record GetAIsQuery : IRequest<PaginatedList<AIDto>>
{
    public string? Title { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetAIsQueryValidator : AbstractValidator<GetAIsQuery>
{
    public GetAIsQueryValidator()
    {
    }
}

public class GetAIsQueryHandler : IRequestHandler<GetAIsQuery, PaginatedList<AIDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetAIsQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<AIDto>> Handle(GetAIsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AIs.Where(x => x.CreatedBy == _user.Id);

        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => x.Title != null && x.Title.Contains(request.Title));
        
        return await PaginatedList<AIDto>.CreateAsync(
            query.OrderByDescending(x => x.Created).ProjectTo<AIDto>(_mapper.ConfigurationProvider),
            request.Page, request.PageSize, cancellationToken);
    }
}
