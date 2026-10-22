using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.IntegrationEvents;
using SurveillanceCameras.Domain.Constants;

namespace SurveillanceCameras.Application.Local.Movies.Commands.ResyncAllMovies;

/// <summary>
/// Chạy nền (tác vụ định kỳ): đưa mọi kênh TikTok đã lưu (đang bật tự đồng bộ) vào hàng đợi Kafka để kiểm tra — drama mới
/// được tạo thành phim, phim đã có được cập nhật lượt xem/thông tin và thêm tập mới. Mỗi kênh rồi mỗi phim là một sự kiện
/// nên lỗi của một kênh/phim không kéo theo cái khác, lỗi tạm thời được Kafka tự thử lại, nhịp gọi TikTok do consumer giữ.
/// </summary>
public record ResyncAllMoviesCommand : IRequest<ResyncAllMoviesResult>;

/// <param name="Channels">Số kênh đang bật tự đồng bộ.</param>
/// <param name="Queued">Số kênh đã đưa vào hàng đợi.</param>
public record ResyncAllMoviesResult(int Channels, int Queued);

public class ResyncAllMoviesCommandHandler : IRequestHandler<ResyncAllMoviesCommand, ResyncAllMoviesResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ResyncAllMoviesCommandHandler> _logger;

    public ResyncAllMoviesCommandHandler(IApplicationDbContext context, ILogger<ResyncAllMoviesCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResyncAllMoviesResult> Handle(ResyncAllMoviesCommand request, CancellationToken cancellationToken)
    {
        // Sự kiện gắn vào bản ghi kênh (OutboxInterceptor lấy sự kiện từ mọi entity đang được theo dõi).
        var channels = await _context.UserProfiles
            .Where(x => x.AutoSyncMovies && x.SecUid != null && x.SecUid != ""
                        && x.UserProfileType!.Code == UserProfileTypeCodes.TikTok)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var channel in channels)
            channel.AddIntegrationEvent(new ChannelSyncRequestedIntegrationEvent { OwnerId = channel.CreatedBy, UserProfileId = channel.Id },
                cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tự đồng bộ phim: đã đưa {Channels} kênh vào hàng đợi kiểm tra", channels.Count);
        return new ResyncAllMoviesResult(channels.Count, channels.Count);
    }
}
