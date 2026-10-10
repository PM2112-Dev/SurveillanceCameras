namespace SurveillanceCameras.Application.Repository.CrawlStory.DTOs;

/// <summary>Một truyện trong danh sách của trang nguồn.</summary>
public class StoryCrawlDto
{
    public string? Title { get; init; }

    public string? Link { get; init; }

    public string? Author { get; init; }

    public string? CoverUrl { get; init; }

    public string? Status { get; init; }

    public int? ChapterCount { get; init; }

    /// <summary>Truyện đã được đồng bộ vào StorySource của người dùng (không phải dữ liệu của trang nguồn).</summary>
    public bool IsSynced { get; set; }
}
