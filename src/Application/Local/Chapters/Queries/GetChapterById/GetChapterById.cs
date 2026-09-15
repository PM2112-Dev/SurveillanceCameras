using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Chapters.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.Chapters.Queries.GetChapterById;

public record GetChapterByIdQuery(int Id) : IRequest<ChapterDto>;

public class GetChapterByIdQueryValidator : AbstractValidator<GetChapterByIdQuery>
{
    public GetChapterByIdQueryValidator()
    {
    }
}

public class GetChapterByIdQueryHandler : IRequestHandler<GetChapterByIdQuery, ChapterDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetChapterByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<ChapterDto> Handle(GetChapterByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Chapters
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);
        
        return _mapper.Map<ChapterDto>(entity);
    }
}
