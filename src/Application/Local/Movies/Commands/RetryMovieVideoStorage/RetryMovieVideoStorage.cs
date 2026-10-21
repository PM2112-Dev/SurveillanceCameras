using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.RetryMovieVideoStorage;

/// <summary>Cho tác vụ nền thử lưu lại video của các tập đã lỗi trong một phim (xoá lỗi và số lần đã thử).</summary>
/// <returns>Số tập được đưa lại vào hàng chờ.</returns>
public record RetryMovieVideoStorageCommand(int MovieId) : IRequest<int>;

public class RetryMovieVideoStorageCommandHandler : IRequestHandler<RetryMovieVideoStorageCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public RetryMovieVideoStorageCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(RetryMovieVideoStorageCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Works.AnyAsync(
            x => x.Id == request.MovieId && x.CreatedBy == _user.Id && x.Type == WorkType.Movie, cancellationToken);
        if (!exists) throw new NotFoundException(request.MovieId.ToString(), "Movie");

        var episodes = await _context.Episodes
            .Where(x => x.WorkId == request.MovieId && x.StreamUrl == null && x.StorageAttempts > 0)
            .ToListAsync(cancellationToken);

        foreach (var episode in episodes)
        {
            episode.StorageAttempts = 0;
            episode.StorageError = null;
            episode.QueuedAt = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return episodes.Count;
    }
}
