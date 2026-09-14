namespace SurveillanceCameras.Application.Local.StorySources.Queries.DTOs;

public class StorySourceVm
{
    IReadOnlyCollection<StorySourceDto> StorySources { get; set; } = new List<StorySourceDto>();
}
