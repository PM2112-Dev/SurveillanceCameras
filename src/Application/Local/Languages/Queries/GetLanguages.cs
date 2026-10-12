using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;

namespace SurveillanceCameras.Application.Local.Languages.Queries;

public record GetLanguagesQuery : IRequest<PaginatedList<LanguageDto>>
{
    /// <summary>Tìm trong tên hoặc mã.</summary>
    public string? Title { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public class GetLanguagesQueryValidator : AbstractValidator<GetLanguagesQuery>
{
    public GetLanguagesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public class GetLanguagesQueryHandler : IRequestHandler<GetLanguagesQuery, PaginatedList<LanguageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetLanguagesQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public Task<PaginatedList<LanguageDto>> Handle(GetLanguagesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Languages.Where(x => x.CreatedBy == _user.Id);

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var term = request.Title.Trim().ToLower();
            query = query.Where(x => (x.Title != null && x.Title.ToLower().Contains(term))
                                     || (x.Code != null && x.Code.ToLower().Contains(term)));
        }

        return PaginatedList<LanguageDto>.CreateAsync(
            query.OrderBy(x => x.Title).ThenBy(x => x.Id).ToDto(_context),
            request.Page, request.PageSize, cancellationToken);
    }
}

public record GetLanguageByIdQuery(int Id) : IRequest<LanguageDto>;

public class GetLanguageByIdQueryHandler : IRequestHandler<GetLanguageByIdQuery, LanguageDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetLanguageByIdQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<LanguageDto> Handle(GetLanguageByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Languages
            .Where(x => x.Id == request.Id && x.CreatedBy == _user.Id)
            .ToDto(_context)
            .FirstOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        return entity;
    }
}
