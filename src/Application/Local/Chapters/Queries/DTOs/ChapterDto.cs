using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Chapters.Queries.DTOs;

public class ChapterDto : BaseDto
{
    public int StoryId { get; init; }
    
    public int PromptId { get; init; }
    
    public string? TytChapterId { get; init; }
    
    public string? NameRaw { get; init; }
    
    public string? NameEdit { get; init; }
    
    public int ChapterNumber { get; init; }
    
    public string? ContentRaw { get; init; }
    
    public string? ContentEdit { get; init; }
    
    public bool IsUploaded { get; init; }
    
    public bool IsPublished { get; init; }
    
    public required Prompt Prompt { get; init; }
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Chapter, ChapterDto>();
        }
    }
}
