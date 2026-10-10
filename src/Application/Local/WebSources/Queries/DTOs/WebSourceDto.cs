using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Application.Local.StorySources.Queries.DTOs;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.WebSources.Queries.DTOs;

public class WebSourceDto : BaseDto
{
    public string? BaseUrl { get; set; }

    public WebSourceType? Type { get; set; }

    public bool IsCookie { get; set; }

    public bool IsNeedEdit { get; set; }

    public int? TytAccountId { get; set; }

    public string? TytAccountTitle { get; set; }

    public int? AccountId { get; set; }

    public string? AccountTitle { get; set; }

    public string? CrawlConfig { get; set; }

    public IReadOnlyCollection<StorySourceDto>? StorySources { get; set; }
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<WebSource, WebSourceDto>()
                .ForMember(dest => dest.StorySources, opt => opt.MapFrom(src => src.StorySources))
                .ForMember(dest => dest.AccountTitle, opt => opt.MapFrom(src => src.Account != null ? src.Account.Title : null))
                .ForMember(dest => dest.TytAccountTitle, opt => opt.MapFrom(src => src.TytAccount != null ? src.TytAccount.Title : null));
        }
    }
}
