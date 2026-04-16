namespace SurveillanceCameras.Application.Local.Areas.Commands.CreateArea;

public class CreateAreaCommandValidator : AbstractValidator<CreateAreaCommand>
{
    public CreateAreaCommandValidator()
    {
        RuleFor(v => v.Title)
            .NotEmpty()
            .NotNull()
            .WithMessage("Title is required")
            .MaximumLength(200)
            .WithMessage("Title must not exceed 200 characters");
    }
}
