using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Chapters.Commands.CreateChapter;

public record CreateChapterCommand : IRequest<int>
{
    public int StoryId { get; init; }

    public int PromptId { get; init; }

    public string? Title { get; init; }

    public string? TytChapterId { get; init; }

    public string? NameRaw { get; init; }

    public string? NameEdit { get; init; }

    public int ChapterNumber { get; init; }

    public string? ContentRaw { get; init; }

    public string? ContentEdit { get; init; }

    public bool IsUploaded { get; init; }

    public bool IsPublished { get; init; }
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
        var entity = new Chapter
        {
            Title = request.Title,
            StoryId = request.StoryId,
            PromptId = request.PromptId,
            TytChapterId = request.TytChapterId,
            NameRaw = request.NameRaw,
            NameEdit = request.NameEdit,
            ChapterNumber = request.ChapterNumber,
            ContentRaw = request.ContentRaw,
            ContentEdit = request.ContentEdit,
            IsUploaded = request.IsUploaded,
            IsPublished = request.IsPublished
        };

        _context.Chapters.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
