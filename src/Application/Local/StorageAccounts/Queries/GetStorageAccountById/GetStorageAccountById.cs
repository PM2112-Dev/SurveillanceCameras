using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.StorageAccounts.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.StorageAccounts.Queries.GetStorageAccountById;

public record GetStorageAccountByIdQuery(int Id) : IRequest<StorageAccountDto>;

public class GetStorageAccountByIdQueryHandler : IRequestHandler<GetStorageAccountByIdQuery, StorageAccountDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetStorageAccountByIdQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<StorageAccountDto> Handle(GetStorageAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _context.StorageAccounts
            .Where(x => x.Id == request.Id && x.CreatedBy == _user.Id)
            .ToDtos(_context)
            .FirstOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(request.Id, dto);

        return dto;
    }
}
