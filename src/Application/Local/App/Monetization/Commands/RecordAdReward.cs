using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.App.Monetization.Commands;

/// <summary>
/// AdMob gọi về sau khi người xem xem hết quảng cáo có thưởng (server-side verification). App đặt <c>user_id</c> là ID
/// người xem và <c>custom_data</c> là ID tập muốn mở.
/// </summary>
public record RecordAdRewardCommand(string? QueryString) : IRequest;

public class RecordAdRewardCommandHandler : IRequestHandler<RecordAdRewardCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAdMobRewardVerifier _verifier;

    public RecordAdRewardCommandHandler(IApplicationDbContext context, IAdMobRewardVerifier verifier)
    {
        _context = context;
        _verifier = verifier;
    }

    public async Task Handle(RecordAdRewardCommand request, CancellationToken cancellationToken)
    {
        var reward = await _verifier.VerifyAsync(request.QueryString, cancellationToken);
        // Nút "Kiểm tra URL" trên AdMob gửi callback không có người xem: chữ ký đúng là đủ.
        if (string.IsNullOrWhiteSpace(reward.UserId)) return;
        if (await _context.AppAdRewards.AnyAsync(x => x.TransactionId == reward.TransactionId, cancellationToken)) return;

        _context.AppAdRewards.Add(new AppAdReward
        {
            UserId = reward.UserId,
            EpisodeId = int.TryParse(reward.CustomData, out var episodeId) ? episodeId : null,
            TransactionId = reward.TransactionId,
            AdUnit = reward.AdUnit,
            RewardedAt = reward.Timestamp
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // AdMob gửi lại cùng giao dịch song song: lượt kia đã ghi.
            if (!await _context.AppAdRewards.AsNoTracking().AnyAsync(x => x.TransactionId == reward.TransactionId, cancellationToken))
                throw;
        }
    }
}

internal static class AdRules
{
    /// <summary>Lượt AdMob xác nhận chỉ dùng được trong khoảng này sau khi xem.</summary>
    public static readonly TimeSpan RewardLifetime = TimeSpan.FromHours(1);

    public static DateTimeOffset StartOfDay(DateTimeOffset now) => new(now.UtcDateTime.Date, TimeSpan.Zero);
}
