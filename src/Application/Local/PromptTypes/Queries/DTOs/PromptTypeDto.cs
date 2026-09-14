using SurveillanceCameras.Application.Local.Prompts.Queries.DTOs;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.PromptTypes.Queries.DTOs;

public class PromptTypeDto : BaseDto
{
    public IReadOnlyCollection<PromptDto>? Prompts { get; }
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<PromptType, PromptTypeDto>().ForMember(dest => dest.Prompts, opt => opt.MapFrom(src => src.Prompts));
        }
    }
}
