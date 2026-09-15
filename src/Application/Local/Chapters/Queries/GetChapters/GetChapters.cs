using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Chapters.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.Chapters.Queries.GetChapters;

public record GetChaptersQuery : IRequest<PaginatedList<ChapterDto>>
{
    public int? StoryId { get; init; }
    public string? Title { get; init; }
    public bool? IsUploaded { get; init; }
    public bool? IsPublished { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetChaptersQueryValidator : AbstractValidator<GetChaptersQuery>
{
    public GetChaptersQueryValidator()
    {
    }
}

public class GetChaptersQueryHandler : IRequestHandler<GetChaptersQuery, PaginatedList<ChapterDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetChaptersQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<ChapterDto>> Handle(GetChaptersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Chapters.Where(x => x.CreatedBy == _user.Id);

        if (request.StoryId.HasValue)
            query = query.Where(x => x.StoryId == request.StoryId);
        
        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => x.Title != null && x.Title.Contains(request.Title));

        if (request.IsUploaded.HasValue)
            query = query.Where(x => x.IsUploaded == request.IsUploaded);

        if (request.IsPublished.HasValue)
            query = query.Where(x=> x.IsPublished == request.IsPublished);
        
        return await PaginatedList<ChapterDto>.CreateAsync(
            query.OrderByDescending(x => x.Created).ProjectTo<ChapterDto>(_mapper.ConfigurationProvider),
            request.Page, request.PageSize, cancellationToken);
    }
}
