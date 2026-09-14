using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Chapters.Commands.CreateChapter;

public record CreateChapterCommand : IRequest<int>
{
}

public class CreateChapterCommandValidator : AbstractValidator<CreateChapterCommand>
{
    public CreateChapterCommandValidator()
    {
    }
}

public class CreateChapterCommandHandler : IRequestHandler<CreateChapterCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateChapterCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateChapterCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
