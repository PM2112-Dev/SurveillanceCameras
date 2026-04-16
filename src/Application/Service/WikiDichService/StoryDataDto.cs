namespace SurveillanceCameras.Application.Service.WikiDichService;

public class StoryDataDto
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public int? TotalChapters { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> Genres { get; set; } = new();
    public string? Link { get; set; }
}

