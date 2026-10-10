using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.AccountTypes.Commands.CreateAccountType;

public record CreateAccountTypeCommand : IRequest<int>
{
    public string? Title { get; init; }

    /// <summary>Trang đăng nhập để lấy cookie cho tài khoản thuộc loại này.</summary>
    public string? BaseUrl { get; init; }

    /// <summary>Cookie chỉ có khi đã đăng nhập (vd. TikTok: sessionid); bỏ trống nếu chưa biết.</summary>
    public string? LoginCookieName { get; init; }
}

public class CreateAccountTypeCommandValidator : AbstractValidator<CreateAccountTypeCommand>
{
    public CreateAccountTypeCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);

        RuleFor(x => x.BaseUrl)
            .MaximumLength(2000)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .When(x => !string.IsNullOrWhiteSpace(x.BaseUrl))
            .WithMessage("Base URL phải là địa chỉ http(s) đầy đủ.");

        RuleFor(x => x.LoginCookieName)
            .MaximumLength(200)
            .Matches("^[^\\s;=,]+$").When(x => !string.IsNullOrWhiteSpace(x.LoginCookieName))
            .WithMessage("Tên cookie không được chứa khoảng trắng hoặc các ký tự ; = ,");
    }
}

public class CreateAccountTypeCommandHandler : IRequestHandler<CreateAccountTypeCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateAccountTypeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateAccountTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = new AccountType
        {
            Title = request.Title,
            BaseUrl = NullIfEmpty(request.BaseUrl),
            LoginCookieName = NullIfEmpty(request.LoginCookieName)
        };
        
        _context.AccountTypes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
