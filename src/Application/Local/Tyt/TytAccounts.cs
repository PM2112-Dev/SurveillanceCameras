using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.Tyt;

namespace SurveillanceCameras.Application.Local.Tyt;

/// <summary>Dựng ngữ cảnh gọi TYT (tài khoản + domain lấy từ loại tài khoản) cho tác vụ nền.</summary>
internal static class TytAccounts
{
    /// <param name="ownerId">Chủ của truyện — tài khoản phải cùng chủ (tác vụ nền không có người đăng nhập).</param>
    /// <exception cref="TytException">Chưa chọn tài khoản hoặc loại tài khoản chưa có Base URL.</exception>
    public static async Task<TytContext> ContextAsync(IApplicationDbContext db, int? accountId, string? ownerId,
        CancellationToken cancellationToken)
    {
        var account = accountId is { } id
            ? await db.Accounts
                .AsNoTracking()
                .Include(x => x.AccountType)
                .FirstOrDefaultAsync(x => x.Id == id && x.CreatedBy == ownerId, cancellationToken)
            : null;

        if (account is null)
            throw new TytException("Truyện chưa chọn tài khoản TYT đăng truyện.");
        if (string.IsNullOrWhiteSpace(account.AccountType?.BaseUrl))
            throw new TytException($"Loại tài khoản “{account.AccountType?.Title}” chưa có Base URL (vd. https://tytnovel.info/).");

        return new TytContext(account.Id, account.AccountType.BaseUrl, account.AccountType.LoginCookieName);
    }
}
