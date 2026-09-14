using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Stories.Queries.GetStories;

public record GetStoriesQuery : IRequest
{
}

public class GetStoriesQueryValidator : AbstractValidator<GetStoriesQuery>
{
    public GetStoriesQueryValidator()
    {
    }
}

public class GetStoriesQueryHandler : IRequestHandler<GetStoriesQuery>
{
    private readonly IApplicationDbContext _context;

    public GetStoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(GetStoriesQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
