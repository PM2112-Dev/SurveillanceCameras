using System.Text;
using System.Text.RegularExpressions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Categories;

/// <summary>
/// Gán thể loại lấy từ trang nguồn vào Category đã khai báo trong hệ thống: chỉ nhận thể loại trùng tên (không phân biệt
/// hoa thường, bỏ khoảng trắng thừa); thể loại không có trong hệ thống bị bỏ qua, không tạo Category mới.
/// </summary>
internal sealed partial class CategoryMatcher
{
    private readonly Dictionary<string, Category> _byName;

    private CategoryMatcher(Dictionary<string, Category> byName) => _byName = byName;

    /// <summary>Nạp Category của người dùng (được theo dõi để gán vào StorySource).</summary>
    public static async Task<CategoryMatcher> LoadAsync(IApplicationDbContext context, string? ownerId,
        CancellationToken cancellationToken)
    {
        var categories = await context.Categories
            .Where(x => x.CreatedBy == ownerId && x.Title != null)
            .ToListAsync(cancellationToken);

        var byName = new Dictionary<string, Category>();
        foreach (var category in categories.OrderBy(x => x.Id)) byName.TryAdd(Normalize(category.Title), category);
        return new CategoryMatcher(byName);
    }

    /// <summary>Thêm vào nguồn truyện các Category trùng tên với thể loại; không bỏ Category đang có (có thể gán tay).</summary>
    /// <returns>Số Category được thêm.</returns>
    public int AssignTo(StorySource source, IEnumerable<string>? genres)
    {
        var added = 0;
        foreach (var genre in genres ?? [])
        {
            if (!_byName.TryGetValue(Normalize(genre), out var category)) continue;
            if (source.Categories.Any(x => x == category || (x.Id != 0 && x.Id == category.Id))) continue;

            source.Categories.Add(category);
            added++;
        }

        return added;
    }

    public static string Normalize(string? name)
        => Whitespace().Replace((name ?? "").Normalize(NormalizationForm.FormC), " ").Trim().ToLowerInvariant();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
