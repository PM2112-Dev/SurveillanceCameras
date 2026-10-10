namespace SurveillanceCameras.Application.Repository.CrawlStory.DTOs;

/// <summary>Thông tin chi tiết của truyện trên trang nguồn.</summary>
public class StoryDataDto
{
    public string? Url { get; init; }

    public string? Title { get; init; }

    public string? Author { get; init; }

    /// <summary>Mô tả, mỗi đoạn một dòng.</summary>
    public string? Description { get; init; }

    public string? CoverUrl { get; init; }

    public string? Status { get; init; }

    public List<string> Genres { get; init; } = [];

    public int? ChapterCount { get; init; }
}
