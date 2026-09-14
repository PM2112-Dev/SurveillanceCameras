using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Categories.Queries.DTOs;

/// <summary>
/// Lightweight Category projection with no back-reference to StorySources — safe to embed
/// inside StorySourceDto without triggering a JSON serialization cycle.
/// </summary>
public class CategorySummaryDto : BaseDto
{
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Category, CategorySummaryDto>();
        }
    }
}
