using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.WebSources.Queries.DTOs;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.WebSources.Queries.GetWebSourceById;

public record GetWebSourceIdQuery(int Id) : IRequest<WebSourceDto>;

public class GetWebSourceByIdQueryHandler : IRequestHandler<GetWebSourceIdQuery, WebSourceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetWebSourceByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<WebSourceDto> Handle(GetWebSourceIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.WebSources
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        return _mapper.Map<WebSourceDto>(entity);
    }
}
