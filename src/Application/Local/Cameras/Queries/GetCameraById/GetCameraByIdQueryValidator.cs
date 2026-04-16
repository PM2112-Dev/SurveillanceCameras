using SurveillanceCameras.Application.Local.Cameras.Queries.GetCameraById;

namespace SurveillanceCameras.Application.Cameras.Queries.GetCameraById;

public class GetCameraByIdQueryValidator : AbstractValidator<GetCameraByIdQuery>
{
    public GetCameraByIdQueryValidator()
    {
        RuleFor(v => v.Id)
            .GreaterThan(0);   
    }
}
