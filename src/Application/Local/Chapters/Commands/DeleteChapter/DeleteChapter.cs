using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Chapters.Commands.DeleteChapter;

public record DeleteChapterCommand : IRequest
{
}

public class DeleteChapterCommandValidator : AbstractValidator<DeleteChapterCommand>
{
    public DeleteChapterCommandValidator()
    {
    }
}

public class DeleteChapterCommandHandler : IRequestHandler<DeleteChapterCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteChapterCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteChapterCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
