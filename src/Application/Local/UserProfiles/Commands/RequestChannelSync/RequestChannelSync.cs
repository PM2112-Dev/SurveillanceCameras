using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.IntegrationEvents;

namespace SurveillanceCameras.Application.Local.UserProfiles.Commands.RequestChannelSync;

/// <summary>Kiểm tra ngay phim mới và tập mới của kênh (chạy nền qua Kafka), không chờ lượt định kỳ.</summary>
public record RequestChannelSyncCommand(int Id) : IRequest;

public class RequestChannelSyncCommandHandler : IRequestHandler<RequestChannelSyncCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public RequestChannelSyncCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(RequestChannelSyncCommand request, CancellationToken cancellationToken)
    {
        var channel = await _context.UserProfiles.FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);
        Guard.Against.NotFound(request.Id, channel);

        if (string.IsNullOrWhiteSpace(channel.SecUid))
        {
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.Id), "Kênh chưa có secUid — tra cứu lại kênh TikTok trước.")]);
        }

        channel.AddIntegrationEvent(new ChannelSyncRequestedIntegrationEvent { OwnerId = channel.CreatedBy, UserProfileId = channel.Id },
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
