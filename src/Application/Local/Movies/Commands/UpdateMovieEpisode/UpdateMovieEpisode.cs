using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.UpdateMovieEpisode;

/// <summary>
/// Sửa số tập và tên (mô tả) của một tập phim. Tập đã sửa được đánh dấu để đồng bộ lại từ TikTok không ghi đè. Số tập
/// không được trùng với tập khác của cùng phim.
/// </summary>
public record UpdateMovieEpisodeCommand(int MovieId, int EpisodeId, int EpisodeNumber, string? Name) : IRequest;

public class UpdateMovieEpisodeCommandValidator : AbstractValidator<UpdateMovieEpisodeCommand>
{
    public UpdateMovieEpisodeCommandValidator()
    {
        RuleFor(x => x.EpisodeNumber).InclusiveBetween(1, 100_000).WithMessage("Số tập từ 1 đến 100000.");
        RuleFor(x => x.Name).MaximumLength(2000);
    }
}

public class UpdateMovieEpisodeCommandHandler : IRequestHandler<UpdateMovieEpisodeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateMovieEpisodeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateMovieEpisodeCommand request, CancellationToken cancellationToken)
    {
        var episode = await _context.Episodes.FirstOrDefaultAsync(
            x => x.Id == request.EpisodeId && x.WorkId == request.MovieId
                 && x.Work!.CreatedBy == _user.Id && x.Work.Type == WorkType.Movie, cancellationToken);

        Guard.Against.NotFound(request.EpisodeId, episode);

        if (request.EpisodeNumber != episode.EpisodeNumber
            && await _context.Episodes.AnyAsync(x => x.WorkId == request.MovieId && x.Id != episode.Id
                                                     && x.EpisodeNumber == request.EpisodeNumber, cancellationToken))
        {
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.EpisodeNumber), $"Phim đã có tập số {request.EpisodeNumber} — chọn số khác hoặc xoá tập trùng trước.")]);
        }

        episode.EpisodeNumber = request.EpisodeNumber;
        episode.Title = $"Tập {request.EpisodeNumber}";
        episode.NameRaw = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim();
        episode.IsEdited = true;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
