using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Areas.Queries.Model;

public class AreaDto
{
    public int Id { get; init; }

    public string? Title { get; init; }

    public int? ParentAreaId { get; init; }

    public AreaType AreaType { get; init; }

    public MapType MapType { get; init; }

    public string? PhotoPath { get; init; }

    public double? Longitude { get; init; }

    public double? Latitude { get; init; }

    public int? Zoom { get; init; }

    public string? Note { get; init; }
    
    public string? CreatedBy { get; init; }
    
    public ICollection<Object>? Children { get; init; } = new List<Object>();

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Area, AreaDto>();
        }
    }
}

