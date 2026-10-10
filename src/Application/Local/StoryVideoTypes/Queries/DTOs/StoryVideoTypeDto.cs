using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.StoryVideoTypes.Queries.DTOs;

public class StoryVideoTypeDto : BaseDto
{
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<StoryVideoType, StoryVideoTypeDto>();
        }
    }
}
