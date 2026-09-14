using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Prompts.Queries.GetPromptById;

public record GetPromptByIdQuery : IRequest
{
}

public class GetPromptByIdQueryValidator : AbstractValidator<GetPromptByIdQuery>
{
    public GetPromptByIdQueryValidator()
    {
    }
}

public class GetPromptByIdQueryHandler : IRequestHandler<GetPromptByIdQuery>
{
    private readonly IApplicationDbContext _context;

    public GetPromptByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(GetPromptByIdQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
