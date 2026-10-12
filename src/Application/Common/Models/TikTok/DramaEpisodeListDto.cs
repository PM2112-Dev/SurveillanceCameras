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

    /// <summary>Mã ngôn ngữ TikTok nhận diện từ phần chữ của video (vd. <c>en</c>, <c>vi</c>); <c>un</c> là không xác định.</summary>
    [JsonPropertyName("textLanguage")]
    public string? TextLanguage { get; init; }

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
