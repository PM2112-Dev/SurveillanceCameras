using SurveillanceCameras.Application.Local.StoryVideoTypes.Queries.DTOs;
using SurveillanceCameras.Application.Local.StorySources.Queries.DTOs;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Works.Queries.DTOs;

public class WorkDto : BaseDto
{
    public int? StorySourceId { get; init; }
    
    public int? AccountId { get; init; }

    public int? StoryVideoTypeId { get; init; }

    public StoryVideoTypeDto? StoryVideoType { get; init; }
    
    public string? TytStoryId { get; init; }

    /// <summary>Lỗi của lần tạo truyện trên TYT gần nhất.</summary>
    public string? TytError { get; init; }

    public DateTimeOffset? TytUpdatedAt { get; init; }

    /// <summary>Khi nào đăng chương lên TYT: tay / ngay khi xong / mỗi N giờ / mỗi ngày.</summary>
    public TytUploadMode TytUploadMode { get; init; }

    public int TytUploadBatchSize { get; init; }

    public int TytUploadEveryHours { get; init; }

    public string? TytUploadDailyTime { get; init; }
    
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
            CreateMap<Work, WorkDto>();
        }
    }
}
