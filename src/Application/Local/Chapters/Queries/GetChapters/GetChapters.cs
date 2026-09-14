using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Chapters.Queries.GetChapters;

public record GetChaptersQuery : IRequest
{
}

public class GetChaptersQueryValidator : AbstractValidator<GetChaptersQuery>
{
    public GetChaptersQueryValidator()
    {
    }
}

public class GetChaptersQueryHandler : IRequestHandler<GetChaptersQuery>
{
    private readonly IApplicationDbContext _context;

    public GetChaptersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(GetChaptersQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
