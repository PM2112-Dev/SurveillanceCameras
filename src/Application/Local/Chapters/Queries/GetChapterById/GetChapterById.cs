using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Chapters.Queries.GetChapterById;

public record GetChapterByIdQuery : IRequest
{
}

public class GetChapterByIdQueryValidator : AbstractValidator<GetChapterByIdQuery>
{
    public GetChapterByIdQueryValidator()
    {
    }
}

public class GetChapterByIdQueryHandler : IRequestHandler<GetChapterByIdQuery>
{
    private readonly IApplicationDbContext _context;

    public GetChapterByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(GetChapterByIdQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
