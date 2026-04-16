namespace SurveillanceCameras.Application.Cameras.Queries.Model;

public class CameraVm
{
    public IReadOnlyCollection<CameraDto> Cameras { get; init; } = [];
}
