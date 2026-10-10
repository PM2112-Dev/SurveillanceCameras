using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Constants;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Movies;

internal static class TikTokChannel
{
    /// <summary>Kênh TikTok đã lưu của người dùng, có secUid (bắt buộc để lấy drama).</summary>
    public static async Task<UserProfile> LoadAsync(IApplicationDbContext context, string? ownerId, int userProfileId,
        CancellationToken cancellationToken)
    {
        var channel = await context.UserProfiles
            .AsNoTracking()
            .Include(x => x.UserProfileType)
            .FirstOrDefaultAsync(x => x.Id == userProfileId && x.CreatedBy == ownerId, cancellationToken);

        Guard.Against.NotFound(userProfileId, channel);

        if (channel.UserProfileType?.Code != UserProfileTypeCodes.TikTok || string.IsNullOrWhiteSpace(channel.SecUid))
        {
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(userProfileId), "Chỉ lấy được drama từ kênh TikTok đã lưu (có secUid) — tra cứu lại kênh nếu thiếu.")]);
        }

        return channel;
    }
}
