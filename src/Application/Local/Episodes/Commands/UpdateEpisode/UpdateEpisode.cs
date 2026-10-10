using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Episodes.Commands.UpdateEpisode;

public record UpdateEpisodeCommand : IRequest
{
    public int Id { get; init; }
    
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

public class UpdateEpisodeCommandValidator : AbstractValidator<UpdateEpisodeCommand>
{
    public UpdateEpisodeCommandValidator()
    {
    }
}

public class UpdateEpisodeCommandHandler : IRequestHandler<UpdateEpisodeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateEpisodeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateEpisodeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Episodes
            .FirstOrDefaultAsync(x=> x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        
        entity.Title = request.Title;

        entity.PromptId = request.PromptId;
        entity.TytChapterId = request.TytChapterId;
        entity.NameRaw = request.NameRaw;
        entity.NameEdit = request.NameEdit;
        entity.EpisodeNumber = request.EpisodeNumber;
        entity.ContentRaw = request.ContentRaw;
        entity.ContentEdit = request.ContentEdit;
        entity.IsUploaded = request.IsUploaded;
        entity.IsPublished = request.IsPublished;
        // Người dùng tự sửa nội dung: trạng thái theo nội dung, xoá lỗi của lần xử lý nền trước.
        entity.Status = EpisodeContentStatus.From(request.ContentRaw, request.ContentEdit);
        entity.Error = null;
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
