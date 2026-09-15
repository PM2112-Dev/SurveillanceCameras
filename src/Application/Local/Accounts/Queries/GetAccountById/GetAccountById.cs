using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Accounts.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.Accounts.Queries.GetAccountById;

public record GetAccountByIdQuery(int Id) : IRequest<AccountDto>;

public class GetAccountByIdQueryValidator : AbstractValidator<GetAccountByIdQuery>
{
    public GetAccountByIdQueryValidator()
    {
    }
}

public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetAccountByIdQueryHandler(IApplicationDbContext context,  IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<AccountDto> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Accounts
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        return _mapper.Map<AccountDto>(entity);
    }
}
