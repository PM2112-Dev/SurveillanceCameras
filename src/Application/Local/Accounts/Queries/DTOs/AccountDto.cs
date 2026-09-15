using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Accounts.Queries.DTOs;

public class AccountDto : BaseDto
{
    public int? AccountTypeId { get; init; }
    
    public string? Cookie { get; init; }
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Account, AccountDto>();
        }
    }
}
