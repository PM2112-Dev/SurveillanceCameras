using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Works.Commands.UpdateWork;

public record UpdateWorkCommand : IRequest
{
    public  int Id { get; init; }
    
    public string? Title { get; init; }

    public int? StoryVideoTypeId { get; init; }

    /// <summary>Tài khoản đăng truyện (truyện do AI tạo từ nguồn truyện chưa có, gắn ở đây).</summary>
    public int? AccountId { get; init; }
    
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

public class UpdateWorkCommandValidator : AbstractValidator<UpdateWorkCommand>
{
    public UpdateWorkCommandValidator()
    {
    }
}

public class UpdateWorkCommandHandler : IRequestHandler<UpdateWorkCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateWorkCommandHandler(IApplicationDbContext context,  IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateWorkCommand request, CancellationToken cancellationToken)
    {
        var entity = await  _context.Works
            .FirstOrDefaultAsync(x=> x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);

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
        
        entity.Title = request.Title;
        entity.StoryVideoTypeId = request.StoryVideoTypeId;
        entity.AccountId = request.AccountId;
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
