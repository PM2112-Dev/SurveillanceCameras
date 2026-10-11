using System.Text.Json.Serialization;

namespace SurveillanceCameras.Application.Common.Models.TikTok;

/// <summary>Phản hồi của <c>/api/drama/episode/item_list/</c> (các tập của một drama).</summary>
public class DramaEpisodeListDto
{
    [JsonPropertyName("cursor")]
    public string? Cursor { get; init; }

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; init; }

    /// <summary>0 = thành công (TikTok bỏ trống khi thành công).</summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; init; }

    [JsonPropertyName("status_msg")]
    public string? StatusMessage { get; init; }

    [JsonPropertyName("itemList")]
    public List<DramaEpisodeDto> ItemList { get; init; } = [];
}

/// <summary>Một tập drama — là một video TikTok.</summary>
public class DramaEpisodeDto
{
    /// <summary>Id video TikTok.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("desc")]
    public string? Desc { get; init; }

    [JsonPropertyName("author")]
    public DramaEpisodeAuthorDto? Author { get; init; }

    [JsonPropertyName("video")]
    public DramaEpisodeVideoDto? Video { get; init; }

    [JsonPropertyName("dramaInfo")]
    public DramaEpisodeInfoDto? DramaInfo { get; init; }

    /// <summary>Số tập (1, 2, …).</summary>
    [JsonIgnore]
    public int? EpisodeNumber => DramaInfo?.DramaVideoData?.EpisodeNumber;
}

public class DramaEpisodeAuthorDto
{
    [JsonPropertyName("uniqueId")]
    public string? UniqueId { get; init; }
}

public class DramaEpisodeVideoDto
{
    /// <summary>Thời lượng (giây).</summary>
    [JsonPropertyName("duration")]
    public int Duration { get; init; }

    [JsonPropertyName("cover")]
    public string? Cover { get; init; }

    /// <summary>Link file video (hết hạn sau khoảng 48 giờ). TikTok chỉ trả khi gọi bằng cookie của tài khoản đã đăng nhập.</summary>
    [JsonPropertyName("playAddr")]
    public string? PlayAddr { get; init; }

    /// <summary>Các bản mã hoá của video (codec, bitrate khác nhau); cũng chỉ có khi đã đăng nhập.</summary>
    [JsonPropertyName("bitrateInfo")]
    public List<DramaVideoBitrateDto> BitrateInfo { get; init; } = [];

    /// <summary>
    /// Các link tải file theo thứ tự nên thử: bản H.264 bitrate cao nhất trước (H.265/bytevc1 nhẹ hơn nhưng nhiều máy
    /// Android không phát được trong WebView), rồi link mặc định, rồi các bản còn lại.
    /// </summary>
    public List<string> DownloadUrls()
    {
        var ordered = BitrateInfo.OrderByDescending(x => x.Bitrate).ToList();
        var urls = ordered.Where(x => string.Equals(x.CodecType, "h264", StringComparison.OrdinalIgnoreCase))
            .SelectMany(x => x.PlayAddr?.UrlList ?? [])
            .Append(PlayAddr)
            .Concat(ordered.SelectMany(x => x.PlayAddr?.UrlList ?? []));

        return urls
            .Where(x => Uri.TryCreate(x, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .Select(x => x!)
            .Distinct()
            .ToList();
    }
}

public class DramaVideoBitrateDto
{
    /// <summary><c>h264</c> hoặc <c>bytevc1</c> (H.265).</summary>
    [JsonPropertyName("CodecType")]
    public string? CodecType { get; init; }

    [JsonPropertyName("Bitrate")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long Bitrate { get; init; }

    [JsonPropertyName("PlayAddr")]
    public DramaVideoPlayAddrDto? PlayAddr { get; init; }
}

public class DramaVideoPlayAddrDto
{
    [JsonPropertyName("UrlList")]
    public List<string> UrlList { get; init; } = [];
}

/// <summary>Thông tin drama gắn trên từng tập (giống một mục của drama_list) kèm số tập.</summary>
public class DramaEpisodeInfoDto : DramaDto
{
    [JsonPropertyName("DramaVideoData")]
    public DramaVideoDataDto? DramaVideoData { get; init; }
}

public class DramaVideoDataDto
{
    [JsonPropertyName("EpisodeNumber")]
    public int EpisodeNumber { get; init; }
}
