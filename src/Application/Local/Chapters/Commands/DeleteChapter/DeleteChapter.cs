using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.Chapters.Commands.DeleteChapter;

public record DeleteChapterCommand(int Id) : IRequest;

public class DeleteChapterCommandHandler : IRequestHandler<DeleteChapterCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteChapterCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteChapterCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Chapters
            .FirstOrDefaultAsync(x=> x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        
        _context.Chapters.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
