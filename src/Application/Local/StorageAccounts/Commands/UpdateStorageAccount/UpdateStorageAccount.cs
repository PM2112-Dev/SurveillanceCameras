using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.StorageAccounts.Commands.CreateStorageAccount;

namespace SurveillanceCameras.Application.Local.StorageAccounts.Commands.UpdateStorageAccount;

public record UpdateStorageAccountCommand : IRequest, IStorageAccountSettings
{
    public int Id { get; init; }

    public string? Title { get; init; }

    public string? CloudflareAccountId { get; init; }

    public string? AccessKeyId { get; init; }

    /// <summary>Bỏ trống để giữ khoá đang lưu.</summary>
    public string? SecretAccessKey { get; init; }

    public string? Bucket { get; init; }

    public string? PublicBaseUrl { get; init; }

    public long QuotaBytes { get; init; }

    public bool IsEnabled { get; init; }

    public int Priority { get; init; }
}

public class UpdateStorageAccountCommandValidator : AbstractValidator<UpdateStorageAccountCommand>
{
    public UpdateStorageAccountCommandValidator()
    {
        Include(new StorageAccountSettingsValidator());
    }
}

public class UpdateStorageAccountCommandHandler : IRequestHandler<UpdateStorageAccountCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ISecretProtector _protector;

    public UpdateStorageAccountCommandHandler(IApplicationDbContext context, IUser user, ISecretProtector protector)
    {
        _context = context;
        _user = user;
        _protector = protector;
    }

    public async Task Handle(UpdateStorageAccountCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StorageAccounts
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        var bucket = request.Bucket!.Trim();
        var cloudflareAccountId = request.CloudflareAccountId!.Trim();
        var publicBaseUrl = request.PublicBaseUrl!.Trim().TrimEnd('/');

        // Link phát của các tập đã lưu được dựng từ bucket và địa chỉ công khai: đổi là link cũ chết.
        var movesBucket = bucket != entity.Bucket || cloudflareAccountId != entity.CloudflareAccountId
                                                  || publicBaseUrl != entity.PublicBaseUrl;
        if (movesBucket && await _context.Episodes.AnyAsync(x => x.StorageAccountId == entity.Id, cancellationToken))
        {
            throw new Common.Exceptions.ValidationException([
                new FluentValidation.Results.ValidationFailure(nameof(request.Bucket),
                    "Tài khoản đang lưu video của các tập phim nên không đổi được Account ID, bucket hay địa chỉ công khai. Tạo tài khoản lưu trữ mới thay vì sửa.")
            ]);
        }

        entity.Title = request.Title!.Trim();
        entity.CloudflareAccountId = cloudflareAccountId;
        entity.AccessKeyId = request.AccessKeyId!.Trim();
        if (!string.IsNullOrWhiteSpace(request.SecretAccessKey))
            entity.SecretAccessKey = _protector.Protect(request.SecretAccessKey.Trim());
        entity.Bucket = bucket;
        entity.PublicBaseUrl = publicBaseUrl;
        entity.QuotaBytes = request.QuotaBytes;
        entity.IsEnabled = request.IsEnabled;
        entity.Priority = request.Priority;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
