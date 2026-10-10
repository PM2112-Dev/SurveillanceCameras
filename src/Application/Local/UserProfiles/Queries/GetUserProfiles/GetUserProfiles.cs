using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.UserProfiles.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.UserProfiles.Queries.GetUserProfiles;

public record GetUserProfilesQuery : IRequest<PaginatedList<UserProfileDto>>
{
    /// <summary>Tìm theo tên kênh hoặc handle.</summary>
    public string? Title { get; init; }

    public int? UserProfileTypeId { get; init; }

    /// <summary>Lọc theo mã nền tảng (vd. <c>tiktok</c>), xem <c>UserProfileTypeCodes</c>.</summary>
    public string? UserProfileTypeCode { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }
}

public class GetUserProfilesQueryValidator : AbstractValidator<GetUserProfilesQuery>
{
    public GetUserProfilesQueryValidator()
    {
    }
}

public class GetUserProfilesQueryHandler : IRequestHandler<GetUserProfilesQuery, PaginatedList<UserProfileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetUserProfilesQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<UserProfileDto>> Handle(GetUserProfilesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.UserProfiles.Where(x => x.CreatedBy == _user.Id);

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var term = request.Title.Trim().TrimStart('@');
            query = query.Where(x =>
                (x.Title != null && x.Title.Contains(term)) || (x.UniqueId != null && x.UniqueId.Contains(term)));
        }

        if (request.UserProfileTypeId is { } typeId)
            query = query.Where(x => x.UserProfileTypeId == typeId);

        if (!string.IsNullOrWhiteSpace(request.UserProfileTypeCode))
        {
            var code = request.UserProfileTypeCode.Trim().ToLower();
            query = query.Where(x => x.UserProfileType!.Code == code);
        }

        // Kênh tra cứu gần nhất lên đầu.
        return await PaginatedList<UserProfileDto>.CreateAsync(
            query.OrderByDescending(x => x.LastFetchedAt).ThenByDescending(x => x.Created)
                .ProjectTo<UserProfileDto>(_mapper.ConfigurationProvider),
            request.Page, request.PageSize, cancellationToken);
    }
}
