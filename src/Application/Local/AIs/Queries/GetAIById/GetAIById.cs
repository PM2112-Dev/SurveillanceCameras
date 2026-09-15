using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.AIs.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.AIs.Queries.GetAIById;

public record GetAIByIdQuery(int Id) : IRequest<AIDto>;

public class GetAIByIdQueryValidator : AbstractValidator<GetAIByIdQuery>
{
    public GetAIByIdQueryValidator()
    {
    }
}

public class GetAIByIdQueryHandler : IRequestHandler<GetAIByIdQuery, AIDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetAIByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<AIDto> Handle(GetAIByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.AIs
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        return _mapper.Map<AIDto>(entity);
    }
}
