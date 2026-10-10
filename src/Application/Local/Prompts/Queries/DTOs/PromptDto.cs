using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Prompts.Queries.DTOs;

public class PromptDto : BaseDto
{
    public int PromptTypeId { get; init; }

    public string? PromptTypeTitle { get; init; }

    public string? PromptTypeCode { get; init; }

    public string? Content { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Prompt, PromptDto>()
                .ForMember(dest => dest.PromptTypeTitle, opt => opt.MapFrom(src => src.PromptType!.Title))
                .ForMember(dest => dest.PromptTypeCode, opt => opt.MapFrom(src => src.PromptType!.Code));
        }
    }
}
