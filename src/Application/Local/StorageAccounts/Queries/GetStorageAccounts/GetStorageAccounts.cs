using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.StorageAccounts.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.StorageAccounts.Queries.GetStorageAccounts;

/// <summary>Tài khoản lưu trữ R2 của người dùng kèm thống kê request và số video upload theo tháng.</summary>
public record GetStorageAccountsQuery : IRequest<List<StorageAccountDto>>;

public class GetStorageAccountsQueryHandler : IRequestHandler<GetStorageAccountsQuery, List<StorageAccountDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public GetStorageAccountsQueryHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public Task<List<StorageAccountDto>> Handle(GetStorageAccountsQuery request, CancellationToken cancellationToken)
        => _context.StorageAccounts
            .AsNoTracking()
            .Where(x => x.CreatedBy == _user.Id)
            .OrderBy(x => x.Id)
            .Take(1)
            .ToDtoListAsync(_context, _timeProvider.GetUtcNow(), cancellationToken);
}
