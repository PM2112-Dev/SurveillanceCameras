using SurveillanceCameras.Application.Local.StorySources.Queries.DTOs;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Categories.Queries.DTOs;

public class CategoryDto : BaseDto
{
    public IReadOnlyCollection<StorySourceDto> StorySources { get; init; } = new List<StorySourceDto>();

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Category, CategoryDto>().ForMember(dest => dest.StorySources, opt => opt.MapFrom(src => src.StorySources));
        }
    }
}
