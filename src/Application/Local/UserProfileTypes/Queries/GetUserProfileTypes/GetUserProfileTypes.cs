using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.UserProfileTypes.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.UserProfileTypes.Queries.GetUserProfileTypes;

public record GetUserProfileTypesQuery : IRequest<PaginatedList<UserProfileTypeDto>>
{
    public string? Title { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public class GetUserProfileTypesQueryValidator : AbstractValidator<GetUserProfileTypesQuery>
{
    public GetUserProfileTypesQueryValidator()
    {
    }
}

public class GetUserProfileTypesQueryHandler : IRequestHandler<GetUserProfileTypesQuery, PaginatedList<UserProfileTypeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetUserProfileTypesQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<UserProfileTypeDto>> Handle(GetUserProfileTypesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.UserProfileTypes.Where(x => x.CreatedBy == _user.Id);

        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => x.Title != null && x.Title.Contains(request.Title));

        return await PaginatedList<UserProfileTypeDto>.CreateAsync(
            query.OrderByDescending(x => x.Created).ProjectTo<UserProfileTypeDto>(_mapper.ConfigurationProvider),
            request.Page, request.PageSize, cancellationToken);
    }
}
