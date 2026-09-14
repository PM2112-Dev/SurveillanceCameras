namespace SurveillanceCameras.Application.Local.WebSources.Queries.DTOs;

public class WebSourceVm
{
    IReadOnlyCollection<WebSourceDto> WebSourceDtos { get; init; }  = new List<WebSourceDto>();
}
