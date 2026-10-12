using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.DeleteMovie;

/// <summary>Xoá phim và các tập (chỉ trong hệ thống, không đụng TikTok), kèm file video đã lưu ở kho.</summary>
public record DeleteMovieCommand(int Id) : IRequest;

public class DeleteMovieCommandHandler : IRequestHandler<DeleteMovieCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly EpisodeVideoStorer _videoStorer;

    public DeleteMovieCommandHandler(IApplicationDbContext context, IUser user, EpisodeVideoStorer videoStorer)
    {
        _context = context;
        _user = user;
        _videoStorer = videoStorer;
    }

    public async Task Handle(DeleteMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = await _context.Works.FirstOrDefaultAsync(
            x => x.Id == request.Id && x.CreatedBy == _user.Id && x.Type == WorkType.Movie, cancellationToken);

        Guard.Against.NotFound(request.Id, movie);

        var stored = await _context.Episodes
            .Where(x => x.WorkId == movie.Id && x.StorageAccountId != null)
            .ToListAsync(cancellationToken);
        await _videoStorer.ReleaseAsync(stored, cancellationToken);

        _context.Works.Remove(movie);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
