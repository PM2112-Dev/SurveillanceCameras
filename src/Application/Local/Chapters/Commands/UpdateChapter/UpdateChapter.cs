using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Chapters.Commands.UpdateChapter;

public record UpdateChapterCommand : IRequest
{
}

public class UpdateChapterCommandValidator : AbstractValidator<UpdateChapterCommand>
{
    public UpdateChapterCommandValidator()
    {
    }
}

public class UpdateChapterCommandHandler : IRequestHandler<UpdateChapterCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateChapterCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateChapterCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
