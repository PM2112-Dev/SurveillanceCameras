using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.PromptTypes.Queries.DTOs;

public class PromptTypeDto : BaseDto
{
    public string? Description { get; init; }

    /// <summary>Số prompt thuộc loại này.</summary>
    public int PromptCount { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<PromptType, PromptTypeDto>()
                .ForMember(dest => dest.PromptCount, opt => opt.MapFrom(src => src.Prompts!.Count));
        }
    }
}
