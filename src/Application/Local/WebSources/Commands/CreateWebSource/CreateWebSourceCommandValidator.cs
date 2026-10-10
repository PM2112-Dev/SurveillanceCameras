using SurveillanceCameras.Application.Repository.CrawlStory.Config;
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

        RuleFor(x => x.Type).IsInEnum();

        RuleFor(x => x.AccountId)
            .NotNull()
            .When(x => x.IsCookie)
            .WithMessage("Nguồn web cần cookie thì phải chọn tài khoản cung cấp cookie.");

        RuleFor(x => x.CrawlConfig)
            .Must(json => CrawlConfig.GetError(json!) is null)
            .When(x => !string.IsNullOrWhiteSpace(x.CrawlConfig))
            .WithMessage((_, json) => $"Cấu hình crawl không hợp lệ: {CrawlConfig.GetError(json!)}");
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
