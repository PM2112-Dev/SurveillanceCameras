using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.AIs.Queries.DTOs;

/// <summary>API key không bao giờ trả ra ngoài, chỉ có <see cref="ApiKeyHint"/> (4 ký tự cuối) để nhận biết key nào.</summary>
public class AIDto : BaseDto
{
    public AIProvider Provider { get; init; }

    public string? Model { get; init; }

    public string? ApiKeyHint { get; init; }

    public string? Project { get; init; }

    public string? Email { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<AI, AIDto>()
                .ForMember(dest => dest.ApiKeyHint, opt => opt.MapFrom(src =>
                    src.ApiKey == null || src.ApiKey.Length < 8
                        ? null
                        : src.ApiKey.Substring(src.ApiKey.Length - 4)));
        }
    }
}
