using FluentValidation;
using SurveillanceCameras.Application.Local.WebSources.Commands.CreateWebSource;

public class CreateWebSourceCommandValidator : AbstractValidator<CreateWebSourceCommand>
{
    public CreateWebSourceCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(500)
            .WithMessage("Title must not exceed 500 characters");

        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .WithMessage("BaseUrl is required")
            .MaximumLength(2000)
            .WithMessage("BaseUrl must not exceed 2000 characters")
            .Must(BeAValidUrl)
            .WithMessage("BaseUrl must be a valid URL");
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
