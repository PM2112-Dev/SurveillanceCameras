using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.StorageAccounts.Commands.DeleteStorageAccount;

/// <summary>Xoá tài khoản lưu trữ khỏi hệ thống (không đụng tới bucket). Còn tập phim lưu video ở đó thì không xoá được.</summary>
public record DeleteStorageAccountCommand(int Id) : IRequest;

public class DeleteStorageAccountCommandHandler : IRequestHandler<DeleteStorageAccountCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteStorageAccountCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteStorageAccountCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StorageAccounts
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        var episodes = await _context.Episodes.CountAsync(x => x.StorageAccountId == entity.Id, cancellationToken);
        if (episodes > 0)
        {
            throw new Common.Exceptions.ValidationException([
                new FluentValidation.Results.ValidationFailure(nameof(request.Id),
                    $"Tài khoản đang lưu video của {episodes} tập phim. Tắt tài khoản để ngừng lưu thêm, hoặc xoá các phim đó trước.")
            ]);
        }

        _context.StorageAccounts.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
