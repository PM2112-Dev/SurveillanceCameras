using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.StorySources.Commands.UpdateStorySource;

public record UpdateStorySourceCommand : IRequest
{
    public int Id { get; init; }

    public string? Title { get; init; }

    public int WebSourceId { get; init; }

    public short StorySourceType { get; init; }

    public string? SinoVietnamese { get; init; }

    public string? Author { get; init; }

    public string? Description { get; init; }

    public string? LinkRaw { get; init; }

    public string? Status { get; init; }

    public DateTimeOffset LastUpdate { get; set; }

    public string? ImageUrl { get; init; }

    public int? TotalChapters { get; init; }

    public ICollection<int>? CategoryIds { get; init; } = new List<int>();
}

public class UpdateStorySourceCommandValidator : AbstractValidator<UpdateStorySourceCommand>
{
    public UpdateStorySourceCommandValidator()
    {
    }
}

public class UpdateStorySourceCommandHandler : IRequestHandler<UpdateStorySourceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateStorySourceCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateStorySourceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StorySources
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Title = request.Title;
        entity.WebSourceId = request.WebSourceId;
        entity.StorySourceType = request.StorySourceType;
        entity.SinoVietnamese = request.SinoVietnamese;
        entity.Author = request.Author;
        entity.Description = request.Description;
        entity.LinkRaw = request.LinkRaw;
        entity.LastUpdate = request.LastUpdate;
        entity.ImageUrl = request.ImageUrl;
        entity.TotalChapters = request.TotalChapters;
        entity.Status = request.Status;

        entity.Categories = request.CategoryIds is { Count: > 0 }
            ? await _context.Categories
                .Where(x => request.CategoryIds.Contains(x.Id) && x.CreatedBy == _user.Id)
                .ToListAsync(cancellationToken)
            : [];

        await _context.SaveChangesAsync(cancellationToken);
    }
}
