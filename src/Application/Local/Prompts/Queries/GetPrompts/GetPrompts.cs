using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Common.Security;
using SurveillanceCameras.Application.Local.Prompts.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.Prompts.Queries.GetPrompts;

[Authorize]
public record GetPromptsQuery : IRequest<PaginatedList<PromptDto>>
{
    public int? PromptTypeId { get; init; }
    public string? Title { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetPromptsQueryValidator : AbstractValidator<GetPromptsQuery>
{
    public GetPromptsQueryValidator()
    {
    }
}

public class GetPromptsQueryHandler : IRequestHandler<GetPromptsQuery, PaginatedList<PromptDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetPromptsQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<PromptDto>> Handle(GetPromptsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Prompts.Where(x => x.CreatedBy == _user.Id);

        if (request.PromptTypeId is not null)
            query = query.Where(x => x.PromptTypeId == request.PromptTypeId);

        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => x.Title != null && x.Title.Contains(request.Title));

        return await PaginatedList<PromptDto>.CreateAsync(
            query.OrderByDescending(x => x.Created)
                .ProjectTo<PromptDto>(_mapper.ConfigurationProvider),
            request.Page,
            request.PageSize,
            cancellationToken
        );
    }
}
