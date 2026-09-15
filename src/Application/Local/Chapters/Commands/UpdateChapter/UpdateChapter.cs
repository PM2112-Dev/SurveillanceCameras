using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Chapters.Commands.UpdateChapter;

public record UpdateChapterCommand : IRequest
{
    public int Id { get; init; }
    
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

public class UpdateChapterCommandValidator : AbstractValidator<UpdateChapterCommand>
{
    public UpdateChapterCommandValidator()
    {
    }
}

public class UpdateChapterCommandHandler : IRequestHandler<UpdateChapterCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateChapterCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateChapterCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Chapters
            .FirstOrDefaultAsync(x=> x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        
        entity.Title = request.Title;
        entity.StoryId = request.StoryId;
        entity.PromptId = request.PromptId;
        entity.TytChapterId = request.TytChapterId;
        entity.NameRaw = request.NameRaw;
        entity.NameEdit = request.NameEdit;
        entity.ChapterNumber = request.ChapterNumber;
        entity.ContentRaw = request.ContentRaw;
        entity.ContentEdit = request.ContentEdit;
        entity.IsUploaded = request.IsUploaded;
        entity.IsPublished = request.IsPublished;
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
