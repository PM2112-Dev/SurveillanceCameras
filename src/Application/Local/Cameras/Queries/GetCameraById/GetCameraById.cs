using SurveillanceCameras.Application.Cameras.Queries.Model;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Security;

namespace SurveillanceCameras.Application.Local.Cameras.Queries.GetCameraById;

[Authorize]
public record GetCameraByIdQuery(int Id) : IRequest<CameraDto>;

public class GetCameraByIdQueryHandler : IRequestHandler<GetCameraByIdQuery, CameraDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetCameraByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<CameraDto> Handle(GetCameraByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Cameras
            .AsNoTracking()
            .Where(camera => camera.Id == request.Id & camera.CreatedBy ==  _user.Id);
        
        var camera = await query
            .ProjectTo<CameraDto>(_mapper.ConfigurationProvider)
            .SingleOrDefaultAsync(cancellationToken);
        
        Guard.Against.Null(camera, nameof(GetCameraByIdQuery));
        
        return camera;
    }
}
