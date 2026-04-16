using SurveillanceCameras.Application.Areas.Queries.Model;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Security;

namespace SurveillanceCameras.Application.Areas.Queries.GetAreas;

[Authorize]
public record GetAreasQuery : IRequest<AreasVm>;

public class GetAreasQueryHandler : IRequestHandler<GetAreasQuery, AreasVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;
    
    public GetAreasQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<AreasVm> Handle(GetAreasQuery request, CancellationToken cancellationToken)
    {
        return new AreasVm
        {
            Areas = await _context.Areas
                .AsNoTracking()
                .Where(area => area.CreatedBy ==  _user.Id)
                .ProjectTo<AreaDto>(_mapper.ConfigurationProvider)
                .OrderBy(x => x.Title)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken)
        };
    }
}
