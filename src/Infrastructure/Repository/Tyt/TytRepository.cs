using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Repository.AccountSession;
using SurveillanceCameras.Application.Repository.Tyt;

namespace SurveillanceCameras.Infrastructure.Repository.Tyt;

/// <summary>
/// Thao tác trên trang "Truyện Của Tôi" của TYT bằng phiên Chrome của tài khoản: TYT chặn mọi request không phải trình
/// duyệt thật (Cloudflare), nên không gọi bằng HttpClient.
/// </summary>
public sealed class TytRepository : ITytRepository
{
    /// <summary>Chặn vòng lặp vô hạn nếu TYT bỏ qua tham số trang.</summary>
    private const int MaxListPages = 100;

    private readonly IAccountSessionManagerRepository _sessions;

    public TytRepository(IAccountSessionManagerRepository sessions)
    {
        _sessions = sessions;
    }

    public async Task<string> CreateStoryAsync(TytContext context, TytCreateStoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var title = request.Title.Trim();
        if (title.Length < 3) throw new TytException("Tên truyện tối thiểu 3 ký tự.");

        // Ghi nhận truyện trùng tên đang có để nhận ra đúng truyện mới nếu TYT không chuyển hướng tới trang truyện.
        var before = await GetStoriesAsync(context, TytStoryVisibility.All, cancellationToken);

        var response = await SendAsync(context, "mystory/new",
        [
            new("title", title),
            new("author", request.Author?.Trim() ?? ""),
            new("agree", "1")
        ], ajax: false, cancellationToken);

        if (TytHtml.StoryIdFromUrl(response.Url) is { } createdId) return createdId;

        var after = await GetStoriesAsync(context, TytStoryVisibility.All, cancellationToken);
        var known = before.Select(x => x.Id).ToHashSet();
        var created = after
            .Where(x => !known.Contains(x.Id) && TytHtml.NormalizeTitle(x.Title) == TytHtml.NormalizeTitle(title))
            .MaxBy(x => x.Id, StringComparer.Ordinal);

        return created?.Id
               ?? throw new TytException(TytHtml.ErrorMessage(response.Body) is { } error
                   ? $"TYT không tạo truyện: {error}"
                   : "TYT không tạo truyện (không thấy truyện mới trong danh sách).");
    }

    public async Task<IReadOnlyList<TytStoryItemDto>> GetStoriesAsync(TytContext context,
        TytStoryVisibility visibility = TytStoryVisibility.All, CancellationToken cancellationToken = default)
    {
        var filter = visibility switch
        {
            TytStoryVisibility.Public => "publish=every-one",
            TytStoryVisibility.OnlyMe => "publish=only-me",
            TytStoryVisibility.Deleted => "publish=deleted",
            _ => null
        };

        var stories = new List<TytStoryItemDto>();
        var seen = new HashSet<string>();

        for (var page = 1; page <= MaxListPages; page++)
        {
            var query = string.Join('&', new[] { filter, page > 1 ? $"page={page}" : null }.OfType<string>());
            var response = await SendAsync(context, "mystory" + (query.Length > 0 ? "?" + query : ""), null, false,
                cancellationToken);

            var items = TytHtml.ParseStoryList(response.Body);
            var added = items.Where(x => seen.Add(x.Id)).ToList();
            if (added.Count == 0) break;   // hết trang (hoặc TYT không phân trang và trả lại trang cũ)

            stories.AddRange(added);
        }

        // Id kiểu ObjectId tăng theo thời gian tạo.
        return stories.OrderBy(x => x.Id, StringComparer.Ordinal).ToList();
    }

    public async Task<TytStoryDto> GetStoryAsync(TytContext context, string storyId,
        CancellationToken cancellationToken = default)
    {
        var id = RequireStoryId(storyId);
        var response = await SendAsync(context, $"mystory/{id}/", null, false, cancellationToken);

        if (response.StatusCode == 404 || TytHtml.StoryIdFromUrl(response.Url) != id)
            throw new TytException($"Không tìm thấy truyện {id} trong tài khoản TYT.");

        try
        {
            return TytHtml.ParseStory(response.Body, id);
        }
        catch (FormatException)
        {
            throw new TytException($"Không đọc được thông tin truyện {id} trên TYT (giao diện trang có thể đã đổi).");
        }
    }

    public async Task UpdateStoryAsync(TytContext context, string storyId, TytUpdateStoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var id = RequireStoryId(storyId);
        // Form của TYT gửi đủ mọi trường: trường không đổi lấy lại từ giá trị hiện tại để không bị xoá trắng.
        var current = await GetStoryAsync(context, id, cancellationToken);

        var title = (request.Title ?? current.Title ?? "").Trim();
        if (title.Length < 3) throw new TytException("Tên truyện tối thiểu 3 ký tự.");

        var response = await SendAsync(context, $"mystory/{id}/update",
        [
            new("title", title),
            new("cover", request.CoverUrl ?? current.CoverUrl ?? ""),
            new("desc", request.Description is null ? current.DescriptionHtml ?? "" : TytHtml.DescriptionToHtml(request.Description)),
            new("genre", TytHtml.GenresValue(request.Genres ?? current.Genres)),
            new("author", request.Author ?? current.Author ?? ""),
            new("status", TytHtml.StatusValue(request.Status ?? current.Status)),
            new("type", ((int)(request.Type ?? current.Type)).ToString())
        ], ajax: true, cancellationToken);

        var (success, message) = TytHtml.ParseAjaxResult(response.Body);
        if (!success)
            throw new TytException($"TYT không cập nhật truyện {id}: {message ?? $"phản hồi không hợp lệ (HTTP {response.StatusCode})"}");
    }

    public async Task<string?> GetStoryIdAsync(TytContext context, string title, CancellationToken cancellationToken = default)
    {
        var normalized = TytHtml.NormalizeTitle(title);
        if (normalized.Length == 0) return null;

        var stories = await GetStoriesAsync(context, TytStoryVisibility.All, cancellationToken);
        return stories
            .Where(x => TytHtml.NormalizeTitle(x.Title) == normalized)
            .MaxBy(x => x.Id, StringComparer.Ordinal)
            ?.Id;
    }

    public async Task CheckSessionAsync(TytContext context, CancellationToken cancellationToken = default)
        // SendAsync ném TytException khi chưa đăng nhập / hết phiên / TYT lỗi 5xx.
        => await SendAsync(context, "mystory", null, false, cancellationToken);

    public async Task<IReadOnlyList<TytGenreDto>> GetGenresAsync(TytContext context, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(context, "the-loai", null, false, cancellationToken);
        var genres = TytHtml.ParseGenres(response.Body);
        if (genres.Count == 0)
            throw new TytException("Không đọc được danh sách thể loại từ TYT (giao diện trang /the-loai có thể đã đổi).");

        return genres;
    }

    public async Task<TytEarningDto> GetEarningAsync(TytContext context, string storyId, CancellationToken cancellationToken = default)
    {
        var id = RequireStoryId(storyId);
        var response = await SendAsync(context, $"mystory/{id}/earning", null, false, cancellationToken);
        return TytHtml.ParseEarning(response.Body);
    }

    public async Task<bool> IsStoryPublicAsync(TytContext context, string storyId, CancellationToken cancellationToken = default)
    {
        var id = RequireStoryId(storyId);
        var response = await SendAsync(context, $"mystory/{id}/publish", null, false, cancellationToken);
        return TytHtml.IsPublishedPublic(response.Body);
    }

    public async Task<IReadOnlyList<TytChapterItemDto>> GetChaptersAsync(TytContext context, string storyId, CancellationToken cancellationToken = default)
    {
        var id = RequireStoryId(storyId);
        var chapters = new List<TytChapterItemDto>();
        var seen = new HashSet<string>();

        for (var page = 1; page <= MaxListPages; page++)
        {
            var response = await SendAsync(context, $"mystory/{id}/chapters" + (page > 1 ? $"?page={page}" : ""), null, false, cancellationToken);
            var added = TytHtml.ParseChapterList(response.Body).Where(x => seen.Add(x.Id)).ToList();
            if (added.Count == 0) break;   // hết trang (hoặc TYT trả lại trang cũ)

            chapters.AddRange(added);
        }

        return chapters;
    }

    public async Task<string?> CreateChapterAsync(TytContext context, string storyId, TytChapterRequest request,
        CancellationToken cancellationToken = default)
    {
        var id = RequireStoryId(storyId);
        var title = request.Title.Trim();
        if (title.Length is < 1 or > 200) title = title.Length == 0 ? $"Chương {request.Number}" : title[..200];
        if (request.Number is < 1 or > 9999) throw new TytException($"Số chương {request.Number} ngoài khoảng 1–9999 của TYT.");

        var content = TytHtml.ChapterContentToHtml(request.Content);
        if (content.Length == 0) throw new TytException("Chương không có nội dung để đăng.");

        // Form "Thêm chương" của TYT gửi ajax tới mystory/{id}/chapters/add (khảo sát 2026-10-22).
        var response = await SendAsync(context, $"mystory/{id}/chapters/add",
        [
            new("title", title),
            new("number", request.Number.ToString()),
            new("content", content),
            new("published", request.Published ? "1" : "0")
        ], ajax: true, cancellationToken);

        var (success, message, redirectUrl) = TytHtml.ParseChapterResult(response.Body);
        if (!success)
        {
            throw new TytException($"TYT không đăng chương {request.Number}: " +
                                   (message ?? $"phản hồi không hợp lệ (HTTP {response.StatusCode})"));
        }

        if (TytHtml.ChapterIdFromUrl(redirectUrl, id) is { } chapterId) return chapterId;

        // TYT không luôn trả đường dẫn chương: tìm trong danh sách chương theo số chương để vẫn lưu được Id.
        try
        {
            return (await GetChaptersAsync(context, id, cancellationToken)).FirstOrDefault(x => x.Number == request.Number)?.Id;
        }
        catch (TytException)
        {
            return null;   // chương đã đăng; chỉ thiếu Id
        }
    }

    public async Task<string?> RequestPublishAsync(TytContext context, string storyId, CancellationToken cancellationToken = default)
    {
        var id = RequireStoryId(storyId);

        // Giá trị của lựa chọn "Công Khai" do TYT cấp cho từng truyện nên đọc từ trang Xuất bản thay vì đoán.
        var page = await SendAsync(context, $"mystory/{id}/publish", null, false, cancellationToken);
        var value = TytHtml.PublicVisibilityValue(page.Body)
                    ?? throw new TytException("Không tìm thấy lựa chọn “Công Khai” ở trang Xuất bản của TYT (giao diện có thể đã đổi).");

        var response = await SendAsync(context, $"mystory/{id}/published", [new("published", value)], ajax: true, cancellationToken);

        var (success, message) = TytHtml.ParseAjaxResult(response.Body);
        if (!success)
            throw new TytException($"TYT không nhận yêu cầu công khai truyện {id}: {message ?? $"phản hồi không hợp lệ (HTTP {response.StatusCode})"}");

        return message;
    }

    public Task UpdateChapterAsync(TytContext context, string storyId, string chapterId, TytChapterRequest request,
        CancellationToken cancellationToken = default)
    {
        RequireStoryId(storyId);
        // Endpoint sửa chương chưa khảo sát được (tài khoản TYT chưa có chương nào để xem form sửa) — không đoán để tránh ghi sai.
        throw new TytException("Chưa hỗ trợ sửa chương đã đăng trên TYT (chưa khảo sát form sửa chương).");
    }

    private async Task<BrowserFetchResponse> SendAsync(TytContext context, string path,
        IReadOnlyList<KeyValuePair<string, string>>? form, bool ajax, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(context.BaseUrl, UriKind.Absolute, out var baseUri))
            throw new TytException("Loại tài khoản TYT chưa có Base URL hợp lệ (vd. https://tytnovel.info/).");

        // Base URL có thể là trang đăng nhập; các trang quản lý truyện nằm ở gốc domain.
        var url = $"{baseUri.GetLeftPart(UriPartial.Authority)}/{path}";
        var target = new AccountLoginTarget(context.BaseUrl, context.LoginCookieName);

        BrowserFetchResponse response;
        try
        {
            response = await _sessions.FetchAsync(context.AccountId, target, new BrowserFetchRequest(url, form, ajax),
                cancellationToken);
        }
        catch (BrowserFetchException ex)
        {
            throw new TytException(ex.Message, ex);
        }

        if (response.StatusCode is 401 or 403
            || response.Url.Contains("/login", StringComparison.OrdinalIgnoreCase)
            || (!ajax && response.StatusCode == 200 && !TytHtml.IsLoggedIn(response.Body)))
        {
            throw new TytException("Tài khoản chưa đăng nhập TYT (hoặc phiên đã hết hạn) — mở phiên đăng nhập của tài khoản và đăng nhập lại.");
        }

        if (response.StatusCode >= 500)
            throw new TytException($"TYT lỗi {response.StatusCode} khi gọi {url}.");

        return response;
    }

    private static string RequireStoryId(string storyId)
    {
        var id = storyId.Trim().ToLowerInvariant();
        return id.Length == 24 && id.All(Uri.IsHexDigit)
            ? id
            : throw new TytException($"Id truyện TYT không hợp lệ: {storyId}");
    }
}
