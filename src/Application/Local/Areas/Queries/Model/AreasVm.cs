namespace SurveillanceCameras.Application.Areas.Queries.Model;

public class AreasVm
{
    public IReadOnlyCollection<AreaDto> Areas { get; init; } = [];
}
