using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.AccountTypes.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.AccountTypes.Queries.GetAccountTypeById;

public record GetAccountTypeByIdQuery(int Id) : IRequest<AccountTypeDto>;

public class GetAccountTypeByIdQueryValidator : AbstractValidator<GetAccountTypeByIdQuery>
{
    public GetAccountTypeByIdQueryValidator()
    {
    }
}

public class GetAccountTypeByIdQueryHandler : IRequestHandler<GetAccountTypeByIdQuery, AccountTypeDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetAccountTypeByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<AccountTypeDto> Handle(GetAccountTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.AccountTypes
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        return _mapper.Map<AccountTypeDto>(entity);
    }
}
