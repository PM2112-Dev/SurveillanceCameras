using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.StorageAccounts.Queries.DTOs;

namespace SurveillanceCameras.Application.Local.StorageAccounts.Commands.UpdateStorageAccountMonthlyStat;

/// <summary>
/// Đối soát / cập nhật số liệu tháng của tài khoản lưu trữ R2 (ví dụ khớp lại số request Class A, Class B hoặc số video
/// theo trang Cloudflare R2 Analytics).
/// </summary>
public record UpdateStorageAccountMonthlyStatCommand : IRequest<StorageAccountMonthlyStatDto>
{
    public int StorageAccountId { get; init; }

    public int Year { get; init; }

    public int Month { get; init; }

    public int VideosUploaded { get; init; }

    public long BytesUploaded { get; init; }

    public int VideosDeleted { get; init; }

    public long BytesDeleted { get; init; }

    public long PutObjectRequests { get; init; }

    public long ListObjectsRequests { get; init; }

    public long OtherClassARequests { get; init; }

    public long GetObjectRequests { get; init; }

    public long HeadObjectRequests { get; init; }

    public long OtherClassBRequests { get; init; }

    public long DeleteObjectRequests { get; init; }
}

public class UpdateStorageAccountMonthlyStatCommandValidator : AbstractValidator<UpdateStorageAccountMonthlyStatCommand>
{
    public UpdateStorageAccountMonthlyStatCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2020, 2100).WithMessage("Năm không hợp lệ.");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("Tháng phải từ 1 đến 12.");
        RuleFor(x => x.VideosUploaded).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BytesUploaded).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VideosDeleted).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BytesDeleted).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PutObjectRequests).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ListObjectsRequests).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OtherClassARequests).GreaterThanOrEqualTo(0);
        RuleFor(x => x.GetObjectRequests).GreaterThanOrEqualTo(0);
        RuleFor(x => x.HeadObjectRequests).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OtherClassBRequests).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DeleteObjectRequests).GreaterThanOrEqualTo(0);
    }
}

public class UpdateStorageAccountMonthlyStatCommandHandler
    : IRequestHandler<UpdateStorageAccountMonthlyStatCommand, StorageAccountMonthlyStatDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateStorageAccountMonthlyStatCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<StorageAccountMonthlyStatDto> Handle(UpdateStorageAccountMonthlyStatCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _context.StorageAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.StorageAccountId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.StorageAccountId, account);

        var stat = await _context.GetOrCreateMonthlyStatAsync(account.Id, request.Year, request.Month, cancellationToken);

        stat.VideosUploaded = request.VideosUploaded;
        stat.BytesUploaded = request.BytesUploaded;
        stat.VideosDeleted = request.VideosDeleted;
        stat.BytesDeleted = request.BytesDeleted;
        stat.PutObjectRequests = request.PutObjectRequests;
        stat.ListObjectsRequests = request.ListObjectsRequests;
        stat.OtherClassARequests = request.OtherClassARequests;
        stat.GetObjectRequests = request.GetObjectRequests;
        stat.HeadObjectRequests = request.HeadObjectRequests;
        stat.OtherClassBRequests = request.OtherClassBRequests;
        stat.DeleteObjectRequests = request.DeleteObjectRequests;

        await _context.SaveChangesAsync(cancellationToken);

        return StorageAccountMonthlyStatDto.From(stat, account.UsedBytes);
    }
}
