using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Common.Security;
using SurveillanceCameras.Application.Local.PromptTypes.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.PromptTypes.Queries.GetPromptTypes;

[Authorize]
public record GetPromptTypesQuery : IRequest<PaginatedList<PromptTypeDto>>
{
    public string? Title { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetPromptTypesQueryValidator : AbstractValidator<GetPromptTypesQuery>
{
    public GetPromptTypesQueryValidator()
    {
    }
}

public class GetPromptTypesQueryHandler : IRequestHandler<GetPromptTypesQuery, PaginatedList<PromptTypeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetPromptTypesQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<PromptTypeDto>> Handle(GetPromptTypesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.PromptTypes.Where(x => x.CreatedBy == _user.Id);

        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => x.Title != null && x.Title.Contains(request.Title));

        return await PaginatedList<PromptTypeDto>.CreateAsync(
            query.OrderByDescending(x => x.Created)
                .ProjectTo<PromptTypeDto>(_mapper.ConfigurationProvider),
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
