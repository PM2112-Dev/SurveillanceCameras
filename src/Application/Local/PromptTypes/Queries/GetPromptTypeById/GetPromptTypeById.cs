using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.PromptTypes.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.PromptTypes.Queries.GetPromptTypeById;

public record GetPromptTypeByIdQuery(int Id) : IRequest<PromptTypeDto>;

public class GetPromptTypeByIdQueryValidator : AbstractValidator<GetPromptTypeByIdQuery>
{
    public GetPromptTypeByIdQueryValidator()
    {
    }
}

public class GetPromptTypeByIdQueryHandler : IRequestHandler<GetPromptTypeByIdQuery, PromptTypeDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetPromptTypeByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PromptTypeDto> Handle(GetPromptTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.PromptTypes
            .Include(x => x.Prompts)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        
        return _mapper.Map<PromptTypeDto>(entity);
    }
}
