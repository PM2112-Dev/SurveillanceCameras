using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Prompts.Queries.DTOs;

public class PromptDto : BaseDto
{
    public string? Content { get; init; }
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Prompt, PromptDto>();
        }
    }
}
