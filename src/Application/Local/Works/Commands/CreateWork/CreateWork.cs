using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Works.Commands.CreateWork;

public record CreateWorkCommand : IRequest<int>
{
    public string? Title { get; init; }
    
    public int StorySourceId { get; init; }
    
    /// <summary>Tài khoản đăng truyện; có thể gắn sau.</summary>
    public int? AccountId { get; init; }

    public int? StoryVideoTypeId { get; init; }
    
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

public class CreateWorkCommandValidator : AbstractValidator<CreateWorkCommand>
{
    public CreateWorkCommandValidator()
    {
    }
}

public class CreateWorkCommandHandler : IRequestHandler<CreateWorkCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateWorkCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(CreateWorkCommand request, CancellationToken cancellationToken)
    {
        if (request.AccountId is { } accountId)
        {
            var accountExists = await _context.Accounts
                .AnyAsync(x => x.Id == accountId && x.CreatedBy == _user.Id, cancellationToken);

            if (!accountExists) throw new NotFoundException(accountId.ToString(), nameof(Account));
        }

        if (request.StoryVideoTypeId is { } storyVideoTypeId)
        {
            var videoTypeExists = await _context.StoryVideoTypes
                .AnyAsync(x => x.Id == storyVideoTypeId && x.CreatedBy == _user.Id, cancellationToken);

            if (!videoTypeExists) throw new NotFoundException(storyVideoTypeId.ToString(), nameof(StoryVideoType));
        }

        var entity = new Work
        {
            Title = request.Title,
            StorySourceId = request.StorySourceId,
            AccountId = request.AccountId,
            StoryVideoTypeId = request.StoryVideoTypeId,
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
        
        _context.Works.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
