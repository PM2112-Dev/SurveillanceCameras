using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.AccountTypes.Queries.DTOs;

public class AccountTypeDto : BaseDto
{
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<AccountType, AccountTypeDto>();
        }
    }
}
