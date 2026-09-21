using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Service.TikTokApi;

namespace SurveillanceCameras.Application.Local.Accounts.Queries.GetTikTokUserProfile;

public record GetTikTokUserProfileQuery(int AccountId, string Handle) : IRequest<TikTokUserProfileDto>;

public class GetTikTokUserProfileQueryValidator : AbstractValidator<GetTikTokUserProfileQuery>
{
    public GetTikTokUserProfileQueryValidator()
    {
        // Handle TikTok: chữ, số, dấu chấm, gạch dưới, tối đa 24 ký tự. Chặn ký tự lạ vì handle được ghép vào URL.
        RuleFor(x => x.Handle)
            .NotEmpty()
            .Matches("^@?[A-Za-z0-9._]{1,24}$")
            .WithMessage("Handle TikTok không hợp lệ (chỉ gồm chữ, số, '.', '_' và tối đa 24 ký tự).");
    }
}

public class GetTikTokUserProfileQueryHandler : IRequestHandler<GetTikTokUserProfileQuery, TikTokUserProfileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ITikTokProfileService _profileService;

    public GetTikTokUserProfileQueryHandler(
        IApplicationDbContext context, IUser user, ITikTokProfileService profileService)
    {
        _context = context;
        _user = user;
        _profileService = profileService;
    }

    public async Task<TikTokUserProfileDto> Handle(GetTikTokUserProfileQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.AccountId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.AccountId, entity);

        return await _profileService.GetUserProfileAsync(entity.Cookie, request.Handle, cancellationToken);
    }
}
