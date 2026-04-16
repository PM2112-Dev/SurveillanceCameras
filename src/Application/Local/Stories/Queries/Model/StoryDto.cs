using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Stories.Queries.Model;

public class StoryDto
{
    public int Id { get; init; }
    
    public string? Title { get; init; }
    
    public string? TitleRaw { get; init; }
    
    public string? StoryWebId { get; init; }
    
    public string? LinkRaw { get; init; }
    
    public string? Author { get; init; }
    
    public string? ImageUrl { get; init; }
    
    public string? DescriptionRaw { get; init; }
    
    public string? DescriptionEdit { get; init; }
    
    public string? LinkChapterOne { get; init; }
    
    public List<string>? Genres { get; init; }
    
    public bool IsScraped { get; init; }
    
    public bool IsEdited { get; init; }
    
    public bool IsComment { get; init; }

    public Publish Publish { get; init; }
    
    public int Uploaded { get; init; }
    
    public bool IsFull { get; init; }
    
    public int EarnCount { get; init; }
    
    public int EarnNow { get; init; }
    
    public int AdCount { get; init; }
    
    public int Paid { get; init; }
    
    public bool IsEarning { get; init; }
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Story, StoryDto>();
        }
    }
}
