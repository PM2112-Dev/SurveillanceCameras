using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.AccountTypes.Queries.DTOs;

public class AccountTypeDto : BaseDto
{
    public string? BaseUrl { get; init; }

    public string? LoginCookieName { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<AccountType, AccountTypeDto>();
        }
    }
}
