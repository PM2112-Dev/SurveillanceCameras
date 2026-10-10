using SurveillanceCameras.Application.Repository.CrawlStory.Config;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.WebSources.Commands.UpdateWebSource;

public record UpdateWebSourceCommand : IRequest
{
    public int Id { get; init; }

    public string? Title { get; init; }

    public string? BaseUrl { get; init; }

    public WebSourceType? Type { get; init; }

    public bool IsCookie { get; init; }

    /// <summary>Tài khoản cung cấp cookie; bắt buộc khi <see cref="IsCookie"/> bật, bị bỏ qua khi tắt.</summary>
    public int? AccountId { get; init; }

    /// <summary>Truyện từ nguồn này cần AI biên tập lại; tắt thì giữ nguyên nội dung gốc.</summary>
    public bool IsNeedEdit { get; init; } = true;

    /// <summary>Tài khoản TYT tự đăng truyện tạo từ nguồn này; để trống thì không đăng.</summary>
    public int? TytAccountId { get; init; }

    /// <summary>Cấu hình crawl dạng JSON; bỏ trống thì dùng repository viết bằng code theo <see cref="Type"/>.</summary>
    public string? CrawlConfig { get; init; }
}

public class UpdateWebSourceCommandValidator : AbstractValidator<UpdateWebSourceCommand>
{
    public UpdateWebSourceCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);

        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .MaximumLength(2000)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
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
}

public class UpdateWebSourceCommandHandler : IRequestHandler<UpdateWebSourceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateWebSourceCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateWebSourceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.WebSources.FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id,
            cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        if (request.IsCookie)
        {
            var accountExists = await _context.Accounts
                .AnyAsync(x => x.Id == request.AccountId && x.CreatedBy == _user.Id, cancellationToken);

            if (!accountExists) throw new NotFoundException(request.AccountId.ToString()!, nameof(Account));
        }

        await EnsureOwnAccountAsync(request.TytAccountId, cancellationToken);

        entity.Title = request.Title;
        entity.BaseUrl = request.BaseUrl;
        entity.Type = request.Type;
        entity.IsCookie = request.IsCookie;
        entity.AccountId = request.IsCookie ? request.AccountId : null;
        entity.IsNeedEdit = request.IsNeedEdit;
        entity.TytAccountId = request.TytAccountId;
        entity.CrawlConfig = string.IsNullOrWhiteSpace(request.CrawlConfig) ? null : request.CrawlConfig;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureOwnAccountAsync(int? accountId, CancellationToken cancellationToken)
    {
        if (accountId is null) return;

        var exists = await _context.Accounts.AnyAsync(x => x.Id == accountId && x.CreatedBy == _user.Id, cancellationToken);
        if (!exists) throw new NotFoundException(accountId.ToString()!, nameof(Account));
    }
}
