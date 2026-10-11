using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.StorageAccounts.Commands.CreateStorageAccount;

public record CreateStorageAccountCommand : IRequest<int>, IStorageAccountSettings
{
    public string? Title { get; init; }

    public string? CloudflareAccountId { get; init; }

    public string? AccessKeyId { get; init; }

    public string? SecretAccessKey { get; init; }

    public string? Bucket { get; init; }

    public string? PublicBaseUrl { get; init; }

    public long QuotaBytes { get; init; } = StorageAccount.DefaultQuotaBytes;

    public bool IsEnabled { get; init; } = true;

    public int Priority { get; init; }
}

/// <summary>Phần cấu hình chung của lệnh tạo và sửa tài khoản lưu trữ.</summary>
public interface IStorageAccountSettings
{
    string? Title { get; }
    string? CloudflareAccountId { get; }
    string? AccessKeyId { get; }
    string? SecretAccessKey { get; }
    string? Bucket { get; }
    string? PublicBaseUrl { get; }
    long QuotaBytes { get; }
}

public class StorageAccountSettingsValidator : AbstractValidator<IStorageAccountSettings>
{
    public StorageAccountSettingsValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Chưa nhập tên tài khoản.").MaximumLength(500);
        RuleFor(x => x.CloudflareAccountId)
            .NotEmpty().WithMessage("Chưa nhập Account ID của Cloudflare.")
            .Matches("^[A-Za-z0-9]{1,64}$").WithMessage("Account ID của Cloudflare chỉ gồm chữ và số.");
        RuleFor(x => x.AccessKeyId).NotEmpty().WithMessage("Chưa nhập Access Key ID.").MaximumLength(200);
        RuleFor(x => x.SecretAccessKey).MaximumLength(500);
        // Quy tắc đặt tên bucket của R2.
        RuleFor(x => x.Bucket)
            .NotEmpty().WithMessage("Chưa nhập tên bucket.")
            .Matches("^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$").WithMessage("Tên bucket gồm 3–63 ký tự: chữ thường, số, gạch nối.");
        RuleFor(x => x.PublicBaseUrl)
            .NotEmpty().WithMessage("Chưa nhập địa chỉ công khai của bucket.")
            .MaximumLength(500)
            .Must(url => Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
                                                                                   && string.IsNullOrEmpty(uri.Query))
            .WithMessage("Địa chỉ công khai phải là link https đầy đủ (vd. https://pub-xxxx.r2.dev).");
        RuleFor(x => x.QuotaBytes).GreaterThan(0).WithMessage("Hạn mức phải lớn hơn 0.");
    }
}

public class CreateStorageAccountCommandValidator : AbstractValidator<CreateStorageAccountCommand>
{
    public CreateStorageAccountCommandValidator()
    {
        Include(new StorageAccountSettingsValidator());
        RuleFor(x => x.SecretAccessKey).NotEmpty().WithMessage("Chưa nhập Secret Access Key.");
    }
}

public class CreateStorageAccountCommandHandler : IRequestHandler<CreateStorageAccountCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ISecretProtector _protector;

    public CreateStorageAccountCommandHandler(IApplicationDbContext context, ISecretProtector protector)
    {
        _context = context;
        _protector = protector;
    }

    public async Task<int> Handle(CreateStorageAccountCommand request, CancellationToken cancellationToken)
    {
        var entity = new StorageAccount
        {
            Title = request.Title!.Trim(),
            CloudflareAccountId = request.CloudflareAccountId!.Trim(),
            AccessKeyId = request.AccessKeyId!.Trim(),
            SecretAccessKey = _protector.Protect(request.SecretAccessKey!.Trim()),
            Bucket = request.Bucket!.Trim(),
            PublicBaseUrl = request.PublicBaseUrl!.Trim().TrimEnd('/'),
            QuotaBytes = request.QuotaBytes,
            IsEnabled = request.IsEnabled,
            Priority = request.Priority
        };

        _context.StorageAccounts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
