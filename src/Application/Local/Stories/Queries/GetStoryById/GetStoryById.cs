using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Stories.Queries.GetStoryById;

public record GetStoryByIdQuery : IRequest
{
}

public class GetStoryByIdQueryValidator : AbstractValidator<GetStoryByIdQuery>
{
    public GetStoryByIdQueryValidator()
    {
    }
}

public class GetStoryByIdQueryHandler : IRequestHandler<GetStoryByIdQuery>
{
    private readonly IApplicationDbContext _context;

    public GetStoryByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(GetStoryByIdQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
