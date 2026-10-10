namespace SurveillanceCameras.Domain.Entities;

public class AccountType : BaseAuditableEntity
{
    /// <summary>Trang đăng nhập: mở trong Chrome trên server để người dùng đăng nhập và lấy cookie cho tài khoản.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Tên cookie chỉ có khi đã đăng nhập (vd. TikTok: <c>sessionid</c>). Có thì hệ thống biết lúc nào đăng nhập
    /// xong/bị đăng xuất và không ghi đè cookie cũ bằng cookie chưa đăng nhập.
    /// </summary>
    public string? LoginCookieName { get; set; }

    public ICollection<Account>? Accounts { get; set; }
}
