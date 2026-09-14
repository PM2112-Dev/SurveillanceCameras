using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.StorySources.Commands.CreateStorySource;

public record CreateStorySourceCommand : IRequest<int>
{
    public string? Title { get; init; }
    
    public int WebSourceId  { get; init; }
    
    public short StorySourceType  { get; init; }
    
    public string? SinoVietnamese  { get; init; }
    
    public string? Author { get; init; }
    
    public string? Description { get; init; }
    
    public string? LinkRaw { get; init; }
    
    public string? Status { get; init; }
    
    public DateTimeOffset LastUpdate { get; set; }
    
    public string? ImageUrl { get; init; }
    
    public int? TotalChapters { get; init; }
    
    public ICollection<int>? CategoryIds { get; init; } = new List<int>();
}

public class CreateStorySourceCommandValidator : AbstractValidator<CreateStorySourceCommand>
{
    public CreateStorySourceCommandValidator()
    {
    }
}

public class CreateStorySourceCommandHandler : IRequestHandler<CreateStorySourceCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateStorySourceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateStorySourceCommand request, CancellationToken cancellationToken)
    {
        var categories = request.CategoryIds != null && request.CategoryIds.Count > 0 
            ? await _context.Categories
                .Where(c => request.CategoryIds.Contains(c.Id))
                .ToListAsync(cancellationToken)
            : [];
        
        var entity = new StorySource
        {
            Title = request.Title,
            WebSourceId = request.WebSourceId,
            StorySourceType = request.StorySourceType,
            SinoVietnamese = request.SinoVietnamese,
            Author = request.Author,
            Description = request.Description,
            LinkRaw = request.LinkRaw,
            Status = request.Status,
            LastUpdate = request.LastUpdate,
            ImageUrl = request.ImageUrl,
            TotalChapters = request.TotalChapters,
            Categories = categories
        };
        
        _context.StorySources.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
