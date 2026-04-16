using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Stories.Commands.UpdateStory;

public record UpdateStoryCommand : IRequest
{
    public int Id { get; init; }
    public string? Title { get; init; }
    
    public string? TitleRaw { get; init; }
    
    public string? StoryWebId { get; init; }
    
    public string? LinkRaw { get; init; }
    
    public string? Author { get; init; }
    
    public int? TotalChapters { get; init; }
    
    public string? ImageUrl { get; init; }
    
    public string? DescriptionRaw { get; init; }
    
    public string? DescriptionEdit { get; init; }
    
    public string? LinkChapterOne { get; init; }
    
    public List<string>? Genres { get; init; }
    
    public bool IsScraped { get; init; }
    
    public bool IsEdited { get; init; }
    
    public bool IsComment { get; init; }

    public Publish Publish { get; init; }
    
    public int Uploaded { get; init; }
    
    public bool IsFull { get; init; }
    
    public int EarnCount { get; init; }
    
    public int EarnNow { get; init; }
    
    public int AdCount { get; init; }
    
    public int Paid { get; init; }
    
    public bool IsEarning { get; init; }
}


public class UpdateStoryCommandHandler : IRequestHandler<UpdateStoryCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateStoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateStoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Stories
            .FindAsync([request.Id], cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Title = request.Title;
        entity.TitleRaw = request.TitleRaw;
        entity.StoryWebId = request.StoryWebId;
        entity.LinkRaw = request.LinkRaw;
        entity.TotalChapters = request.TotalChapters;
        entity.Author = request.Author;
        entity.ImageUrl = request.ImageUrl;
        entity.DescriptionRaw = request.DescriptionRaw;
        entity.DescriptionEdit = request.DescriptionEdit;
        entity.LinkChapterOne = request.LinkChapterOne;
        entity.Genres = request.Genres;
        entity.IsScraped = request.IsScraped;
        entity.IsEdited = request.IsEdited;
        entity.IsComment = request.IsComment;
        entity.Publish = request.Publish;
        entity.Uploaded = request.Uploaded;
        entity.IsFull = request.IsFull;
        entity.EarnCount = request.EarnCount;
        entity.EarnNow = request.EarnNow;
        entity.AdCount = request.AdCount;
        entity.Paid = request.Paid;
        entity.IsEarning = request.IsEarning;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
