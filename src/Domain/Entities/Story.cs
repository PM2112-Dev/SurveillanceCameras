namespace SurveillanceCameras.Domain.Entities;

public class Story : BaseAuditableEntity
{
    public int StorySourceId { get; set; }
    
    public int AccountId { get; set; }
    
    public string? TytStoryId { get; set; }
    
    public string? Description { get; set; }
    
    public string? Author { get; set; }
    
    public bool IsFull { get; set; }
    
    public bool IsPublished { get; set; }
    
    public bool IsEarning { get; set; }
    
    public bool IsComment { get; set; }
    
    public int ChapterUploaded { get; set; }
    
    public int EarnCount { get; set; }
    
    public int EarnNow { get; set; }
    
    public int AdCount { get; set; }
    
    public int Paid { get; set; }
}
