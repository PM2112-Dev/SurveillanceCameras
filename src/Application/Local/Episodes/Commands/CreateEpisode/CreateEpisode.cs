using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Episodes.Commands.CreateEpisode;

public record CreateEpisodeCommand : IRequest<int>
{
    public int WorkId { get; init; }

    public int? PromptId { get; init; }

    public string? Title { get; init; }

    public string? TytChapterId { get; init; }

    public string? NameRaw { get; init; }

    public string? NameEdit { get; init; }

    public int EpisodeNumber { get; init; }

    public string? ContentRaw { get; init; }

    public string? ContentEdit { get; init; }

    public bool IsUploaded { get; init; }

    public bool IsPublished { get; init; }
}

public class CreateEpisodeCommandValidator : AbstractValidator<CreateEpisodeCommand>
{
    public CreateEpisodeCommandValidator()
    {
    }
}

public class CreateEpisodeCommandHandler : IRequestHandler<CreateEpisodeCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateEpisodeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(CreateEpisodeCommand request, CancellationToken cancellationToken)
    {
        var storyExists = await _context.Works
            .AnyAsync(x => x.Id == request.WorkId && x.CreatedBy == _user.Id, cancellationToken);
        if (!storyExists) throw new NotFoundException(request.WorkId.ToString(), nameof(Work));

        var entity = new Episode
        {
            Title = request.Title,
            WorkId = request.WorkId,
            PromptId = request.PromptId,
            TytChapterId = request.TytChapterId,
            NameRaw = request.NameRaw,
            NameEdit = request.NameEdit,
            EpisodeNumber = request.EpisodeNumber,
            ContentRaw = request.ContentRaw,
            ContentEdit = request.ContentEdit,
            IsUploaded = request.IsUploaded,
            IsPublished = request.IsPublished,
            Status = EpisodeContentStatus.From(request.ContentRaw, request.ContentEdit)
        };

        _context.Episodes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
