using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Prompts.Queries.GetPrompts;

public record GetPromptsQuery : IRequest
{
}

public class GetPromptsQueryValidator : AbstractValidator<GetPromptsQuery>
{
    public GetPromptsQueryValidator()
    {
    }
}

public class GetPromptsQueryHandler : IRequestHandler<GetPromptsQuery>
{
    private readonly IApplicationDbContext _context;

    public GetPromptsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(GetPromptsQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
