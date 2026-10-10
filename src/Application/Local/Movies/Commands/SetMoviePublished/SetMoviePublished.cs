using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.SetMoviePublished;

/// <summary>Bật/tắt xuất bản phim lên app xem phim. Bỏ trống <paramref name="Ids"/> thì áp dụng cho mọi phim của người dùng.</summary>
/// <returns>Số phim đã đổi trạng thái.</returns>
public record SetMoviePublishedCommand(bool IsPublished, List<int>? Ids = null) : IRequest<int>;

public class SetMoviePublishedCommandHandler : IRequestHandler<SetMoviePublishedCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SetMoviePublishedCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(SetMoviePublishedCommand request, CancellationToken cancellationToken)
    {
        var query = _context.Works.Where(x => x.CreatedBy == _user.Id && x.Type == WorkType.Movie
                                              && x.IsPublished != request.IsPublished);
        if (request.Ids is { Count: > 0 } ids) query = query.Where(x => ids.Contains(x.Id));

        var movies = await query.ToListAsync(cancellationToken);
        foreach (var movie in movies) movie.IsPublished = request.IsPublished;

        await _context.SaveChangesAsync(cancellationToken);
        return movies.Count;
    }
}
