using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Common.Security;
using SurveillanceCameras.Application.Local.Categories.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.Categories.Queries.GetCategories;

[Authorize]
public record GetCategoriesQuery : IRequest<PaginatedList<CategoryDto>>
{
    public string? Title { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetCategoriesQueryValidator : AbstractValidator<GetCategoriesQuery>
{
    public GetCategoriesQueryValidator()
    {
    }
}

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, PaginatedList<CategoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetCategoriesQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<CategoryDto>> Handle(GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Categories.Where(x => x.CreatedBy == _user.Id);

        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(x => x.Title != null && x.Title.Contains(request.Title));

        return await PaginatedList<CategoryDto>.CreateAsync(
            query.OrderByDescending(x => x.Created).ProjectTo<CategoryDto>(_mapper.ConfigurationProvider),
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
