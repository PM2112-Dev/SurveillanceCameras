using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.VideoStorage;

namespace SurveillanceCameras.Application.Local.StorageAccounts.Commands.CheckStorageAccount;

/// <summary>
/// Kết nối thử tới bucket và đối soát dung lượng: dung lượng đã dùng được đặt lại bằng tổng kích thước mọi file đang có
/// trong bucket (kể cả file không do hệ thống tải lên, vì chúng cũng chiếm hạn mức).
/// </summary>
public record CheckStorageAccountCommand(int Id) : IRequest<CheckStorageAccountResult>;

/// <param name="Error">Lý do không kết nối được; <c>null</c> khi thành công.</param>
public record CheckStorageAccountResult(bool Succeeded, string? Error, long UsedBytes, int Objects);

public class CheckStorageAccountCommandHandler : IRequestHandler<CheckStorageAccountCommand, CheckStorageAccountResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ISecretProtector _protector;
    private readonly IVideoStorageRepository _storage;
    private readonly TimeProvider _timeProvider;

    public CheckStorageAccountCommandHandler(IApplicationDbContext context, IUser user, ISecretProtector protector,
        IVideoStorageRepository storage, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _protector = protector;
        _storage = storage;
        _timeProvider = timeProvider;
    }

    public async Task<CheckStorageAccountResult> Handle(CheckStorageAccountCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StorageAccounts
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        CheckStorageAccountResult result;
        try
        {
            var usage = await _storage.GetUsageAsync(entity.ToTarget(_protector), cancellationToken);
            entity.UsedBytes = usage.Bytes;
            entity.LastError = null;
            result = new CheckStorageAccountResult(true, null, usage.Bytes, usage.Objects);
        }
        catch (VideoStorageException ex)
        {
            entity.LastError = ex.Message;
            result = new CheckStorageAccountResult(false, ex.Message, entity.UsedBytes, 0);
        }

        entity.LastCheckedAt = _timeProvider.GetUtcNow();
        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }
}
