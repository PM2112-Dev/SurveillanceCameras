using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Security;
using SurveillanceCameras.Application.Local.Prompts.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.Prompts.Queries.GetPromptById;

[Authorize]
public record GetPromptByIdQuery(int Id) : IRequest<PromptDto>;

public class GetPromptByIdQueryValidator : AbstractValidator<GetPromptByIdQuery>
{
    public GetPromptByIdQueryValidator()
    {
    }
}

public class GetPromptByIdQueryHandler : IRequestHandler<GetPromptByIdQuery, PromptDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetPromptByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PromptDto> Handle(GetPromptByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Prompts
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        
        return _mapper.Map<PromptDto>(entity);
    }
}
