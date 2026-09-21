using System.Text.Json.Serialization;

namespace SurveillanceCameras.Application.TikTokApi;

/// <summary>Phản hồi của <c>/api/drama/user/drama_list/</c> (danh sách phim/drama của một kênh TikTok).</summary>
public class DramaListDto
{
    /// <summary>Con trỏ trang kế tiếp — truyền lại vào lần gọi sau để lấy trang tiếp theo.</summary>
    [JsonPropertyName("cursor")]
    public string? Cursor { get; init; }

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; init; }

    /// <summary>0 = thành công.</summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; init; }

    [JsonPropertyName("status_msg")]
    public string? StatusMessage { get; init; }

    [JsonPropertyName("dramaList")]
    public List<DramaDto> DramaList { get; init; } = [];
}

public class DramaDto
{
    [JsonPropertyName("dramaID")]
    public string? DramaId { get; init; }

    [JsonPropertyName("dramaName")]
    public string? DramaName { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("numVideos")]
    public int NumVideos { get; init; }

    /// <summary>Lượt xem (TikTok trả về dạng chuỗi).</summary>
    [JsonPropertyName("numWatched")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long NumWatched { get; init; }

    /// <summary>Tổng thời lượng (TikTok trả về dạng chuỗi, đơn vị giây).</summary>
    [JsonPropertyName("totalDuration")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long TotalDuration { get; init; }

    [JsonPropertyName("isLimitedFree")]
    public bool IsLimitedFree { get; init; }

    [JsonPropertyName("cover")]
    public DramaCoverDto? Cover { get; init; }

    /// <summary>Ảnh bìa theo kích thước: khoá là độ rộng ("240", "480", "720", "960").</summary>
    [JsonPropertyName("zoomCover")]
    public Dictionary<string, string> ZoomCover { get; init; } = [];

    [JsonPropertyName("themes")]
    public List<DramaThemeDto> Themes { get; init; } = [];
}

public class DramaCoverDto
{
    [JsonPropertyName("urlList")]
    public List<string> UrlList { get; init; } = [];
}

public class DramaThemeDto
{
    [JsonPropertyName("tagID")]
    public string? TagId { get; init; }

    [JsonPropertyName("tagKey")]
    public string? TagKey { get; init; }

    [JsonPropertyName("tagVal")]
    public string? TagValue { get; init; }
}
