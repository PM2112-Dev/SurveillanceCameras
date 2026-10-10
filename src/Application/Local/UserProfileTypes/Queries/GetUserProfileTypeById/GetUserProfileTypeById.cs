using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.UserProfileTypes.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.UserProfileTypes.Queries.GetUserProfileTypeById;

public record GetUserProfileTypeByIdQuery(int Id) : IRequest<UserProfileTypeDto>;

public class GetUserProfileTypeByIdQueryValidator : AbstractValidator<GetUserProfileTypeByIdQuery>
{
    public GetUserProfileTypeByIdQueryValidator()
    {
    }
}

public class GetUserProfileTypeByIdQueryHandler : IRequestHandler<GetUserProfileTypeByIdQuery, UserProfileTypeDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetUserProfileTypeByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<UserProfileTypeDto> Handle(GetUserProfileTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.UserProfileTypes
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        return _mapper.Map<UserProfileTypeDto>(entity);
    }
}
