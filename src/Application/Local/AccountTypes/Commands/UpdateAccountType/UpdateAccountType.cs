using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.AccountTypes.Commands.UpdateAccountType;

public record UpdateAccountTypeCommand : IRequest
{
    public int Id { get; init; }
    
    public string? Title { get; init; }

    /// <summary>Trang đăng nhập để lấy cookie cho tài khoản thuộc loại này.</summary>
    public string? BaseUrl { get; init; }

    /// <summary>Cookie chỉ có khi đã đăng nhập (vd. TikTok: sessionid); bỏ trống nếu chưa biết.</summary>
    public string? LoginCookieName { get; init; }
}

public class UpdateAccountTypeCommandValidator : AbstractValidator<UpdateAccountTypeCommand>
{
    public UpdateAccountTypeCommandValidator()
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

public class UpdateAccountTypeCommandHandler : IRequestHandler<UpdateAccountTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateAccountTypeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateAccountTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.AccountTypes
            .FirstOrDefaultAsync(x=> x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);
        
        entity.Title = request.Title;
        entity.BaseUrl = NullIfEmpty(request.BaseUrl);
        entity.LoginCookieName = NullIfEmpty(request.LoginCookieName);
        
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
