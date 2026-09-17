using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.StorySources.IntegrationEvents;
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
    
    public string? LastUpdate { get; set; }
    
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
        var categories = request.CategoryIds is { Count: > 0 } 
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
        
        // Two SaveChanges calls inside one transaction: the first assigns the DB-generated Id
        // (needed by the integration event's payload), the second lets OutboxInterceptor persist
        // the OutboxMessage for that event — both commit together, so a crash between them rolls
        // back the whole insert rather than leaving an entity with no corresponding outbox row.
        // await _context.ExecuteInTransactionAsync(async ct =>
        // {
        //     _context.StorySources.Add(entity);
        //     await _context.SaveChangesAsync(ct);
        //
        //     entity.AddIntegrationEvent(new StorySourceCreatedIntegrationEvent
        //     {
        //         StorySourceId = entity.Id,
        //         Title = entity.Title,
        //         WebSourceId = entity.WebSourceId,
        //         LinkRaw = entity.LinkRaw
        //     });
        //     await _context.SaveChangesAsync(ct);
        // }, cancellationToken);

        if (request.LinkRaw is not null)
        {
            await _context.ExecuteInTransactionAsync(async ct =>
                {
                    _context.StorySources.Add(entity);
                    await _context.SaveChangesAsync(ct);
                    
                    
                    
                }, cancellationToken
            );
        }
        else
        {
            _context.StorySources.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
        
        return entity.Id;
    }
}
