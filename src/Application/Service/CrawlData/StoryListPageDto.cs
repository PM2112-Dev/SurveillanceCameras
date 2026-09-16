namespace SurveillanceCameras.Application.CrawlData;

public class StoryListItemDto
{
    public string? Title { get; set; }
    public string? Link { get; set; }
}

public class StoryListPageDto
{
    public List<StoryListItemDto> Stories { get; set; } = new();

    /// <summary>Absolute URL of the next page, or null if this is the last page.</summary>
    public string? NextPageUrl { get; set; }
}
