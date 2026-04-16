using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Stories.Commands.CreateStory;

public record CreateStoryCommand : IRequest<int>
{
    public string? Title { get; init; }
    
    public string? TitleRaw { get; init; }
    
    public string? StoryWebId { get; init; }
    
    public string? LinkRaw { get; init; }
    
    public string? Author { get; init; }
    
    public string? ImageUrl { get; init; }
    
    public int? TotalChapters { get; init; }
    
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

public class CreateStoryCommandHandler : IRequestHandler<CreateStoryCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateStoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateStoryCommand request, CancellationToken cancellationToken)
    {
        var entity = new Story
        {
            Title = request.Title,
            TitleRaw = request.TitleRaw,
            StoryWebId = request.StoryWebId,
            LinkRaw = request.LinkRaw,
            Author = request.Author,
            TotalChapters = request.TotalChapters,
            ImageUrl = request.ImageUrl,
            DescriptionRaw = request.DescriptionRaw,
            DescriptionEdit = request.DescriptionEdit,
            LinkChapterOne = request.LinkChapterOne,
            Genres = request.Genres,
            IsScraped = request.IsScraped,
            IsEdited = request.IsEdited,
            IsComment = request.IsComment,
            Publish = request.Publish,
            EarnCount = request.EarnCount,
            EarnNow = request.EarnNow,
            AdCount = request.AdCount,
            Paid = request.Paid,
            IsEarning = request.IsEarning,
            Uploaded = request.Uploaded,
            IsFull = request.IsFull,
        };

        _context.Stories.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
