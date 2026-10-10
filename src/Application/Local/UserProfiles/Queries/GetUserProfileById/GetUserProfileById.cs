using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.UserProfiles.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.UserProfiles.Queries.GetUserProfileById;

public record GetUserProfileByIdQuery(int Id) : IRequest<UserProfileDto>;

public class GetUserProfileByIdQueryValidator : AbstractValidator<GetUserProfileByIdQuery>
{
    public GetUserProfileByIdQueryValidator()
    {
    }
}

public class GetUserProfileByIdQueryHandler : IRequestHandler<GetUserProfileByIdQuery, UserProfileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetUserProfileByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _context.UserProfiles
            .Where(x => x.Id == request.Id && x.CreatedBy == _user.Id)
            .ProjectTo<UserProfileDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(request.Id, dto);
        return dto;
    }
}
