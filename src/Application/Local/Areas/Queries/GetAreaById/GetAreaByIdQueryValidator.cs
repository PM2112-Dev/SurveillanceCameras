namespace SurveillanceCameras.Application.Areas.Queries.GetAreaById;

public class GetAreaByIdQueryValidator : AbstractValidator<GetAreaByIdQuery>
{
    public GetAreaByIdQueryValidator()
    {
        RuleFor(v => v.Id)
            .GreaterThan(0);
    }
}

