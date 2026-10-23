using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.StorageAccounts.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.StorageAccounts.Queries.GetStorageAccountById;

public record GetStorageAccountByIdQuery(int Id) : IRequest<StorageAccountDto>;

public class GetStorageAccountByIdQueryHandler : IRequestHandler<GetStorageAccountByIdQuery, StorageAccountDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public GetStorageAccountByIdQueryHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<StorageAccountDto> Handle(GetStorageAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _context.StorageAccounts
            .AsNoTracking()
            .Where(x => x.Id == request.Id && x.CreatedBy == _user.Id)
            .ToDtoListAsync(_context, _timeProvider.GetUtcNow(), cancellationToken);

        var dto = list.FirstOrDefault();
        Guard.Against.NotFound(request.Id, dto);

        return dto;
    }
}
