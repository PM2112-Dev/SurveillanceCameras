using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Security;
using SurveillanceCameras.Application.Areas.Queries.GetAreas;
using SurveillanceCameras.Application.Areas.Queries.Model;
using SurveillanceCameras.Domain.Constants;

namespace SurveillanceCameras.Application.Areas.Queries.GetAreaById;

[Authorize]
public record GetAreaByIdQuery(int Id) : IRequest<AreaDto>;

public class GetAreaByIdQueryHandler : IRequestHandler<GetAreaByIdQuery, AreaDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;
    
    public GetAreaByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<AreaDto> Handle(GetAreaByIdQuery request, CancellationToken cancellationToken)
    {

        var query = _context.Areas
            .AsNoTracking()
            .Where(area => area.Id == request.Id & area.CreatedBy ==  _user.Id);

        var area = await query
            .ProjectTo<AreaDto>(_mapper.ConfigurationProvider)
            .SingleOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(request.Id, area);

        return area;
    }
}
