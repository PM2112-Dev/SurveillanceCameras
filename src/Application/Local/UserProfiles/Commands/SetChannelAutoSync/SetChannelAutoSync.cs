using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.UserProfiles.Commands.SetChannelAutoSync;

/// <summary>Bật/tắt tự kiểm tra phim mới và tập mới của kênh theo định kỳ.</summary>
public record SetChannelAutoSyncCommand(int Id, bool Enabled) : IRequest;

public class SetChannelAutoSyncCommandHandler : IRequestHandler<SetChannelAutoSyncCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SetChannelAutoSyncCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(SetChannelAutoSyncCommand request, CancellationToken cancellationToken)
    {
        var channel = await _context.UserProfiles.FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);
        Guard.Against.NotFound(request.Id, channel);

        channel.AutoSyncMovies = request.Enabled;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
