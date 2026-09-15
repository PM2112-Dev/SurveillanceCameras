using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.AIs.Queries.DTOs;

public class AIDto : BaseDto
{
    public string? Model { get; init; }
    public string? ApiKey { get; init; }
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<AI, AIDto>();
        }
    }
}
