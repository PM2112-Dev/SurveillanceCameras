using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Stories.Commands.CreateStory;

public record CreateStoryCommand : IRequest<int>
{
    public string? Title { get; init; }
    
    public int StorySourceId { get; init; }
    
    public int AccountId { get; init; }
    
    public string? TytStoryId { get; init; }
    
    public string? Author { get; init; }
    
    public string? Description { get; init; }
    
    public bool IsFull { get; init; }
    
    public bool IsPublished { get; init; }
    
    public bool IsEarning { get; init; }
    
    public bool IsComment { get; init; }
    
    public int ChapterUploaded { get; init; }
    
    public int EarnCount { get; init; }
    
    public int EarnNow { get; init; }
    
    public int AdCount { get; init; }
    
    public int Paid { get; init; }
}

public class CreateStoryCommandValidator : AbstractValidator<CreateStoryCommand>
{
    public CreateStoryCommandValidator()
    {
    }
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
            StorySourceId = request.StorySourceId,
            AccountId = request.AccountId,
            TytStoryId = request.TytStoryId,
            Description = request.Description,
            Author = request.Author,
            IsFull = request.IsFull,
            IsPublished = request.IsPublished,
            IsEarning = request.IsEarning,
            IsComment = request.IsComment,
            ChapterUploaded = request.ChapterUploaded,
            EarnCount = request.EarnCount,
            EarnNow = request.EarnNow,
            AdCount = request.AdCount,
            Paid = request.Paid
        };
        
        _context.Stories.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
