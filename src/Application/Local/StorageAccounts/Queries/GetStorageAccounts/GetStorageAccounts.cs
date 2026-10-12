using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.StorageAccounts.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.StorageAccounts.Queries.GetStorageAccounts;

/// <summary>Mọi tài khoản lưu trữ của người dùng, theo thứ tự được dùng để lưu video.</summary>
public record GetStorageAccountsQuery : IRequest<List<StorageAccountDto>>;

public class GetStorageAccountsQueryHandler : IRequestHandler<GetStorageAccountsQuery, List<StorageAccountDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetStorageAccountsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public Task<List<StorageAccountDto>> Handle(GetStorageAccountsQuery request, CancellationToken cancellationToken)
        => _context.StorageAccounts
            .Where(x => x.CreatedBy == _user.Id)
            .OrderBy(x => x.Priority).ThenBy(x => x.Id)
            .ToDtos(_context)
            .ToListAsync(cancellationToken);
}
