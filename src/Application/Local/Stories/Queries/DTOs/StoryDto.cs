using SurveillanceCameras.Application.Local.StorySources.Queries.DTOs;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Stories.Queries.DTOs;

public class StoryDto : BaseDto
{
    public int StorySourceId { get; init; }
    
    public int AccountId { get; init; }
    
    public string? TytStoryId { get; init; }
    
    public string? Description { get; init; }
    
    public string? Author { get; init; }
    
    public bool IsFull { get; init; }
    
    public bool IsPublished { get; init; }
    
    public bool IsEarning { get; init; }
    
    public bool IsComment { get; init; }
    
    public int ChapterUploaded { get; init; }
    
    public int EarnCount { get; init; }
    
    public int EarnNow { get; init; }
    
    public int AdCount { get; init; }
    
    public int Paid { get; init; }
    
    public StorySourceDto? StorySource { get; init; }
    
    private class Mapping : Profile
    {
        public  Mapping()
        {
            CreateMap<Story, StoryDto>();
        }
    }
}
