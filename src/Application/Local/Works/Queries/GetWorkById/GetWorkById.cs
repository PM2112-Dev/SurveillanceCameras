using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Works.Queries.DTOs;
using SurveillanceCameras.Application.Local.Works.Queries.GetWorks;

namespace SurveillanceCameras.Application.Local.Works.Queries.GetWorkById;

public record GetWorkByIdQuery(int Id) : IRequest<WorkDto>;

public class GetWorkByIdQueryValidator : AbstractValidator<GetWorkByIdQuery>
{
    public GetWorkByIdQueryValidator()
    {
    }
}

public class GetWorkByIdQueryHandler : IRequestHandler<GetWorkByIdQuery, WorkDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public GetWorkByIdQueryHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public async Task<WorkDto> Handle(GetWorkByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Works
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        return _mapper.Map<WorkDto>(entity);
    }
}
