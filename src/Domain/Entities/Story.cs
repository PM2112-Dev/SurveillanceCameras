namespace SurveillanceCameras.Domain.Entities;

public class Story : BaseAuditableEntity
{
    public string? TitleRaw { get; set; }
    
    public string? StoryWebId { get; set; }
    
    public string? LinkRaw { get; set; }
    
    public string? Author { get; set; }
    
    public int? TotalChapters { get; set; }
    
    public string? ImageUrl { get; set; }
    
    public string? DescriptionRaw { get; set; }
    
    public string? DescriptionEdit { get; set; }
    
    public string? LinkChapterOne { get; set; }
    
    public List<string>? Genres { get; set; }
    
    public bool IsScraped { get; set; }
    
    public bool IsEdited { get; set; }
    
    public bool IsComment { get; set; }

    public Publish Publish { get; set; } = Publish.Private;
    
    public int Uploaded { get; set; }
    
    public bool IsFull { get; set; }
    
    public int EarnCount { get; set; }
    
    public int EarnNow { get; set; }
    
    public int AdCount { get; set; }
    
    public int Paid { get; set; }
    
    public bool IsEarning { get; set; }
}
