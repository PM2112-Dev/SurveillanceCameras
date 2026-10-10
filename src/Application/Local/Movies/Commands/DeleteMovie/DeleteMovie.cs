using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.DeleteMovie;

/// <summary>Xoá phim và các tập (chỉ trong hệ thống, không đụng TikTok).</summary>
public record DeleteMovieCommand(int Id) : IRequest;

public class DeleteMovieCommandHandler : IRequestHandler<DeleteMovieCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteMovieCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = await _context.Works.FirstOrDefaultAsync(
            x => x.Id == request.Id && x.CreatedBy == _user.Id && x.Type == WorkType.Movie, cancellationToken);

        Guard.Against.NotFound(request.Id, movie);

        _context.Works.Remove(movie);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
