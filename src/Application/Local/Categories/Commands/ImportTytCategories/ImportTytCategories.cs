using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Tyt;
using SurveillanceCameras.Application.Repository.Tyt;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Categories.Commands.ImportTytCategories;

/// <summary>
/// Lấy danh sách thể loại của TYT (bằng phiên đăng nhập của tài khoản TYT) và thêm vào thể loại của người dùng. Thể loại đã
/// có (cùng tên, không phân biệt hoa thường/khoảng trắng) thì giữ nguyên — chạy lại nhiều lần không sinh bản trùng.
/// </summary>
public record ImportTytCategoriesCommand(int AccountId) : IRequest<ImportTytCategoriesResult>;

/// <param name="Total">Số thể loại trên TYT.</param>
/// <param name="Added">Số thể loại mới thêm.</param>
/// <param name="Existing">Số thể loại đã có sẵn.</param>
public record ImportTytCategoriesResult(int Total, int Added, int Existing, List<string> AddedTitles);

public class ImportTytCategoriesCommandHandler : IRequestHandler<ImportTytCategoriesCommand, ImportTytCategoriesResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ITytRepository _tyt;

    public ImportTytCategoriesCommandHandler(IApplicationDbContext context, IUser user, ITytRepository tyt)
    {
        _context = context;
        _user = user;
        _tyt = tyt;
    }

    public async Task<ImportTytCategoriesResult> Handle(ImportTytCategoriesCommand request, CancellationToken cancellationToken)
    {
        var context = await TytAccounts.ContextAsync(_context, request.AccountId, _user.Id, cancellationToken);
        var genres = await _tyt.GetGenresAsync(context, cancellationToken);

        var existing = (await _context.Categories
                .Where(x => x.CreatedBy == _user.Id)
                .Select(x => x.Title)
                .ToListAsync(cancellationToken))
            .Select(Normalize)
            .ToHashSet();

        var added = new List<string>();
        foreach (var genre in genres)
        {
            if (!existing.Add(Normalize(genre.Title))) continue;
            _context.Categories.Add(new Category { Title = genre.Title });
            added.Add(genre.Title);
        }

        if (added.Count > 0) await _context.SaveChangesAsync(cancellationToken);
        return new ImportTytCategoriesResult(genres.Count, added.Count, genres.Count - added.Count, added);
    }

    private static string Normalize(string? title) => string.Join(' ', (title ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
}
