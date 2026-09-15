using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Stories.Commands.UpdateStory;

public record UpdateStoryCommand : IRequest
{
    public  int Id { get; init; }
    
    public string? Title { get; init; }
    
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

public class UpdateStoryCommandValidator : AbstractValidator<UpdateStoryCommand>
{
    public UpdateStoryCommandValidator()
    {
    }
}

public class UpdateStoryCommandHandler : IRequestHandler<UpdateStoryCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateStoryCommandHandler(IApplicationDbContext context,  IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateStoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await  _context.Stories
            .FirstOrDefaultAsync(x=> x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);
        
        entity.Title = request.Title;
        entity.TytStoryId = request.TytStoryId;
        entity.Author = request.Author;
        entity.Description = request.Description;
        entity.IsFull = request.IsFull;
        entity.IsPublished = request.IsPublished;
        entity.IsEarning = request.IsEarning;
        entity.IsComment = request.IsComment;
        entity.ChapterUploaded = request.ChapterUploaded;
        entity.EarnCount = request.EarnCount;
        entity.EarnNow = request.EarnNow;
        entity.AdCount = request.AdCount;
        entity.Paid = request.Paid;
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
