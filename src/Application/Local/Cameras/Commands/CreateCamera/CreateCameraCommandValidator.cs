namespace SurveillanceCameras.Application.Cameras.Commands.CreateCamera;

public class CreateCameraCommandValidator : AbstractValidator<CreateCameraCommand>
{
    public CreateCameraCommandValidator()
    {
        RuleFor(v => v.Title)
            .NotEmpty()
            .NotNull()
            .WithMessage("Title is required")
            .MaximumLength(200)
            .WithMessage("Title must not exceed 200 characters");
    }
}
