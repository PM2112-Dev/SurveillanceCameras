using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models.TikTok;
using SurveillanceCameras.Application.Repository.TikTok;
using SurveillanceCameras.Domain.Constants;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.TikTok.Commands.SearchTiktokUserProfile;

/// <summary>
/// Tra cứu kênh TikTok theo <see cref="Handle"/> bằng cookie của Account <see cref="AccountId"/>,
/// rồi lưu/cập nhật thông tin cơ bản của kênh vào <see cref="UserProfile"/>.
/// </summary>
public record SearchTiktokUserProfileCommand : IRequest<TikTokUserProfileDto>
{
    public int AccountId { get; init; }

    public required string Handle { get; init; }
}

public class SearchTiktokUserProfileCommandValidator : AbstractValidator<SearchTiktokUserProfileCommand>
{
    public SearchTiktokUserProfileCommandValidator()
    {
        // Handle TikTok: chữ, số, dấu chấm, gạch dưới, tối đa 24 ký tự. Chặn ký tự lạ vì handle được ghép vào URL.
        RuleFor(x => x.Handle)
            .NotEmpty()
            .Matches("^@?[A-Za-z0-9._]{1,24}$")
            .WithMessage("Handle TikTok không hợp lệ (chỉ gồm chữ, số, '.', '_' và tối đa 24 ký tự).");
    }
}

public class SearchTiktokUserProfileCommandHandler : IRequestHandler<SearchTiktokUserProfileCommand, TikTokUserProfileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ITikTokRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SearchTiktokUserProfileCommandHandler(
        IApplicationDbContext context, IUser user, ITikTokRepository repository, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<TikTokUserProfileDto> Handle(SearchTiktokUserProfileCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.AccountId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.AccountId, account);

        var profile = await _repository.FetchUserProfile(account.Cookie, request.Handle, cancellationToken);

        await SaveProfileAsync(profile, cancellationToken);

        return profile;
    }

    private async Task SaveProfileAsync(TikTokUserProfileDto profile, CancellationToken cancellationToken)
    {
        // Không có UserId thì không nhận ra được kênh ở lần sau, lưu vào sẽ sinh bản trùng.
        if (string.IsNullOrWhiteSpace(profile.UserId)) return;

        var profileType = await _context.UserProfileTypes
            .FirstOrDefaultAsync(x => x.CreatedBy == _user.Id && x.Code == UserProfileTypeCodes.TikTok, cancellationToken);

        // Người dùng chưa khai báo loại TikTok thì tạo sẵn, để tra cứu không bị chặn vì thiếu cấu hình.
        if (profileType is null)
        {
            profileType = new UserProfileType { Title = "TikTok", Code = UserProfileTypeCodes.TikTok };
            _context.UserProfileTypes.Add(profileType);
        }

        // Tìm theo UserId (bất biến); handle có thể bị chủ kênh đổi nên chỉ là dữ liệu cập nhật.
        var entity = profileType.Id == 0
            ? null
            : await _context.UserProfiles.FirstOrDefaultAsync(x =>
                x.CreatedBy == _user.Id && x.UserProfileTypeId == profileType.Id && x.UserId == profile.UserId,
                cancellationToken);

        if (entity is null)
        {
            entity = new UserProfile { UserProfileType = profileType, UserId = profile.UserId };
            _context.UserProfiles.Add(entity);
        }

        entity.Title = profile.Nickname;
        entity.UniqueId = profile.UniqueId;
        entity.SecUid = profile.SecUid;
        entity.Signature = profile.Signature;
        entity.AvatarUrl = profile.AvatarUrl;
        entity.Verified = profile.Verified;
        entity.FollowerCount = profile.FollowerCount;
        entity.FollowingCount = profile.FollowingCount;
        entity.HeartCount = profile.HeartCount;
        entity.VideoCount = profile.VideoCount;
        entity.LastFetchedAt = _timeProvider.GetUtcNow();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
