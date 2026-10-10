using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.SetMovieEpisodeStream;

/// <summary>
/// Gán link file video tự lưu trữ (mp4/HLS) cho một tập phim; bỏ trống để gỡ (app quay lại phát bằng trình phát nhúng của
/// nền tảng nguồn).
/// </summary>
public record SetMovieEpisodeStreamCommand(int MovieId, int EpisodeId, string? StreamUrl) : IRequest;

public class SetMovieEpisodeStreamCommandValidator : AbstractValidator<SetMovieEpisodeStreamCommand>
{
    public SetMovieEpisodeStreamCommandValidator()
    {
        RuleFor(x => x.StreamUrl)
            .MaximumLength(2000)
            .Must(url => string.IsNullOrWhiteSpace(url)
                         || (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Link video phải là link https đầy đủ.");
    }
}

public class SetMovieEpisodeStreamCommandHandler : IRequestHandler<SetMovieEpisodeStreamCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SetMovieEpisodeStreamCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(SetMovieEpisodeStreamCommand request, CancellationToken cancellationToken)
    {
        var episode = await _context.Episodes.FirstOrDefaultAsync(
            x => x.Id == request.EpisodeId && x.WorkId == request.MovieId
                 && x.Work!.CreatedBy == _user.Id && x.Work.Type == WorkType.Movie, cancellationToken);

        Guard.Against.NotFound(request.EpisodeId, episode);

        episode.StreamUrl = string.IsNullOrWhiteSpace(request.StreamUrl) ? null : request.StreamUrl.Trim();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
