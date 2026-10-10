using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;

namespace SurveillanceCameras.Application.Local.App.Queries;

/// <summary>Bình luận của một phim, mới nhất trước.</summary>
public record GetAppCommentsQuery(int MovieId, int Page = 1, int PageSize = 30) : IRequest<PaginatedList<AppCommentDto>>;

public class GetAppCommentsQueryValidator : AbstractValidator<GetAppCommentsQuery>
{
    public GetAppCommentsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetAppCommentsQueryHandler : IRequestHandler<GetAppCommentsQuery, PaginatedList<AppCommentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetAppCommentsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<PaginatedList<AppCommentDto>> Handle(GetAppCommentsQuery request, CancellationToken cancellationToken)
    {
        if (!await _context.PublishedMovies().AnyAsync(x => x.Id == request.MovieId, cancellationToken))
            throw new NotFoundException(request.MovieId.ToString(), "Movie");

        var userId = _user.Id;
        return await PaginatedList<AppCommentDto>.CreateAsync(
            _context.AppComments
                .Where(x => x.WorkId == request.MovieId)
                .OrderByDescending(x => x.Created)
                .ThenByDescending(x => x.Id)
                .Select(x => new AppCommentDto
                {
                    Id = x.Id,
                    Author = x.AuthorName,
                    Content = x.Content,
                    Created = x.Created,
                    IsMine = userId != null && x.CreatedBy == userId
                }),
            request.Page, request.PageSize, cancellationToken);
    }
}
