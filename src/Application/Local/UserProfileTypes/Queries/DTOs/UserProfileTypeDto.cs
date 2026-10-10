using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.UserProfileTypes.Queries.DTOs;

public class UserProfileTypeDto : BaseDto
{
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<UserProfileType, UserProfileTypeDto>();
        }
    }
}
