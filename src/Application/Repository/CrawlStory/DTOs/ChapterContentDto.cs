namespace SurveillanceCameras.Application.Repository.CrawlStory.DTOs;

/// <summary>Nội dung một chương lấy từ trang nguồn.</summary>
public class ChapterContentDto
{
    public string? Url { get; init; }

    public string? Title { get; init; }

    public List<string> Paragraphs { get; init; } = [];
}
