using SurveillanceCameras.Application.Local.StorySources.Queries.DTOs;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.WebSources.Queries.DTOs;

public class WebSourceDto : BaseDto
{
    public string? BaseUrl { get; set; }

    public IReadOnlyCollection<StorySourceDto>? StorySources { get; set; }
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<WebSource, WebSourceDto>()
                .ForMember(dest => dest.StorySources, opt => opt.MapFrom(src => src.StorySources));
        }
    }
}
