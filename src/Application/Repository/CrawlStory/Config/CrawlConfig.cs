using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurveillanceCameras.Application.Repository.CrawlStory.Config;

/// <summary>
/// Mô tả cách lấy dữ liệu của một trang truyện, lưu dạng JSON trong <c>WebSource.CrawlConfig</c>.
/// Thêm nguồn mới chỉ cần khai báo cấu hình này, không phải viết repository.
/// <para>
/// <b>Selector</b> (các thuộc tính kiểu chuỗi bên dưới): <c>"css"</c> lấy text, <c>"css@attr"</c> lấy thuộc tính,
/// <c>"css@html"</c> lấy HTML; bỏ phần css (vd. <c>"@href"</c>) để lấy trên chính phần tử item.
/// Link/ảnh luôn được đổi thành URL tuyệt đối.
/// </para>
/// <para>
/// <b>Template</b> (URL, query, header): các đoạn <c>{...}</c> là biểu thức — biến (<c>url</c>, <c>origin</c>,
/// <c>page</c>, <c>offset</c>, <c>pageSize</c>, biến trong <c>vars</c>), chuỗi <c>'...'</c>, số, nối bằng <c>+</c>,
/// và hàm <c>sha256(x)</c>, <c>md5(x)</c>, <c>rotate(x, n)</c>, <c>lower(x)</c>, <c>upper(x)</c>, <c>urlencode(x)</c>.
/// </para>
/// </summary>
public class CrawlConfig
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Tên tham số query thêm vào mọi request với giá trị ngẫu nhiên (vd. <c>"_"</c> → <c>?_=1727370000000</c>)
    /// để bỏ qua cache CDN. Dùng cho trang mà Cloudflare trả bản cache cũ (wikidichzz cache trang danh sách nhiều ngày);
    /// trang đó phải bỏ qua tham số lạ. Không đặt cho request có ký (tham số thừa có thể làm sai chữ ký).
    /// </summary>
    public string? CacheBuster { get; set; }

    /// <summary>Header gửi kèm mọi request (vd. User-Agent riêng).</summary>
    public Dictionary<string, string>? Headers { get; set; }

    public CrawlStoryListConfig? StoryList { get; set; }

    public CrawlStoryConfig? Story { get; set; }

    public CrawlChapterListConfig? ChapterList { get; set; }

    public CrawlChapterConfig? Chapter { get; set; }

    /// <summary>Đọc cấu hình; ném <see cref="JsonException"/> nếu JSON sai.</summary>
    public static CrawlConfig Parse(string json)
        => JsonSerializer.Deserialize<CrawlConfig>(json, JsonOptions)
           ?? throw new JsonException("Cấu hình crawl rỗng.");

    /// <summary>Thông báo lỗi nếu <paramref name="json"/> không đọc được thành cấu hình, ngược lại <c>null</c>.</summary>
    public static string? GetError(string json)
    {
        try
        {
            Parse(json);
            return null;
        }
        catch (JsonException ex)
        {
            return ex.Message;
        }
    }
}

public class CrawlRequestConfig
{
    /// <summary>URL template; mặc định <c>{url}</c> (URL đang xem).</summary>
    public string? Url { get; set; }

    public Dictionary<string, string>? Query { get; set; }

    public Dictionary<string, string>? Headers { get; set; }
}

public class CrawlPaginationConfig
{
    /// <summary>
    /// URL template cho trang từ 2 trở đi (vd. <c>{url}/trang-{page}</c>). Bỏ trống thì mọi trang dùng
    /// <c>request.url</c> (khi trang được chọn qua query, vd. <c>start={offset}</c>).
    /// </summary>
    public string? PageUrl { get; set; }

    /// <summary>Số item mỗi trang; dùng để tính <c>offset</c> và đoán còn trang sau khi không có selector.</summary>
    public int? PageSize { get; set; }

    /// <summary>Selector các số trong khung phân trang; số lớn nhất là trang cuối.</summary>
    public string? LastPage { get; set; }

    /// <summary>Số lấy từ <see cref="LastPage"/> là offset (vd. <c>data-start</c>) chứ không phải số trang.</summary>
    public bool LastPageIsOffset { get; set; }

    /// <summary>Selector nút "trang sau"; có phần tử thì còn trang sau.</summary>
    public string? NextPage { get; set; }
}

public abstract class CrawlListConfig
{
    public CrawlRequestConfig? Request { get; set; }

    public CrawlPaginationConfig? Pagination { get; set; }

    /// <summary>Selector mỗi phần tử trong danh sách.</summary>
    public string Item { get; set; } = "";

    /// <summary>Trang nguồn xếp ngược (mới nhất trước): đảo thứ tự cả trang lẫn item để hiển thị từ cũ tới mới.</summary>
    public bool Reverse { get; set; }
}

public class CrawlStoryListConfig : CrawlListConfig
{
    public string? Title { get; set; }

    public string? Link { get; set; }

    public string? Author { get; set; }

    public string? Cover { get; set; }

    public string? Status { get; set; }

    public string? ChapterCount { get; set; }
}

public class CrawlStoryConfig
{
    public string? Title { get; set; }

    public string? Author { get; set; }

    public string? Description { get; set; }

    public string? Cover { get; set; }

    public string? Status { get; set; }

    /// <summary>Selector lấy tất cả thể loại (mỗi phần tử là một thể loại).</summary>
    public string? Genres { get; set; }

    public string? ChapterCount { get; set; }
}

public class CrawlChapterListConfig : CrawlListConfig
{
    /// <summary>
    /// Biến lấy bằng regex (nhóm 1) từ HTML trang truyện, dùng trong template request
    /// (vd. <c>bookId</c>, <c>signKey</c> để ký request mục lục).
    /// </summary>
    public Dictionary<string, string>? Vars { get; set; }

    public string? Title { get; set; }

    public string? Link { get; set; }
}

public class CrawlChapterConfig
{
    public string? Title { get; set; }

    /// <summary>Selector phần tử chứa nội dung chương.</summary>
    public string? Content { get; set; }

    /// <summary>Selector các phần tử cần bỏ khỏi nội dung (quảng cáo, nút…); script/style luôn bị bỏ.</summary>
    public List<string>? Remove { get; set; }
}
