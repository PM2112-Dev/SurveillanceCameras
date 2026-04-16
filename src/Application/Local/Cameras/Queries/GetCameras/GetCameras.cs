using SurveillanceCameras.Application.Cameras.Queries.Model;
using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Cameras.Queries.GetCameras;

public record GetCamerasQuery : IRequest<CameraVm>;

public class GetCamerasQueryHandler : IRequestHandler<GetCamerasQuery, CameraVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetCamerasQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<CameraVm> Handle(GetCamerasQuery request, CancellationToken cancellationToken)
    {
        return new CameraVm
        {
            Cameras = await _context.Cameras
                .AsNoTracking()
                .Where(camera => camera.CreatedBy ==  _user.Id)
                .ProjectTo<CameraDto>(_mapper.ConfigurationProvider)
                .OrderBy(x => x.Title)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken)
        };
    }
}
