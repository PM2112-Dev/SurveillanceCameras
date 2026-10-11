using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.SetMovieEpisodeStream;

/// <summary>
/// Gán link file video tự lưu trữ (mp4/HLS) cho một tập phim; bỏ trống để gỡ (app quay lại phát bằng trình phát nhúng của
/// nền tảng nguồn, và tác vụ nền sẽ tải video về kho lại nếu còn tài khoản lưu trữ trống). File hệ thống đã lưu cho tập
/// bị xoá khỏi kho.
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
    private readonly EpisodeVideoStorer _videoStorer;

    public SetMovieEpisodeStreamCommandHandler(IApplicationDbContext context, IUser user, EpisodeVideoStorer videoStorer)
    {
        _context = context;
        _user = user;
        _videoStorer = videoStorer;
    }

    public async Task Handle(SetMovieEpisodeStreamCommand request, CancellationToken cancellationToken)
    {
        var episode = await _context.Episodes.FirstOrDefaultAsync(
            x => x.Id == request.EpisodeId && x.WorkId == request.MovieId
                 && x.Work!.CreatedBy == _user.Id && x.Work.Type == WorkType.Movie, cancellationToken);

        Guard.Against.NotFound(request.EpisodeId, episode);

        var streamUrl = string.IsNullOrWhiteSpace(request.StreamUrl) ? null : request.StreamUrl.Trim();
        if (streamUrl == episode.StreamUrl) return;

        await _videoStorer.ReleaseAsync([episode], cancellationToken);
        episode.StreamUrl = streamUrl;
        episode.StorageError = null;
        episode.StorageAttempts = 0;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
