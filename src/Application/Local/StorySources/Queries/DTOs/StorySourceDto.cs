using SurveillanceCameras.Application.Local.Categories.Queries.DTOs;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.StorySources.Queries.DTOs;

public class StorySourceDto : BaseDto
{
    public int WebSourceId { get; init; }
    public short StorySourceType { get; init; }
    public string? SinoVietnamese { get; init; }
    public string? Author { get; init; }
    public string? Description { get; init; }
    public string? LinkRaw { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset LastUpdate { get; init; }
    public string? ImageUrl { get; init; }
    public int? TotalChapters { get; init; }

    public IReadOnlyCollection<CategorySummaryDto> Categories { get; init; } = new List<CategorySummaryDto>();

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<StorySource, StorySourceDto>()
                .ForMember(dest => dest.Categories,
                    opt => opt.MapFrom(src => src.Categories));
        }
    }
}
