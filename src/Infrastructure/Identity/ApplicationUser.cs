using Microsoft.AspNetCore.Identity;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public int ParentUserId { get; set; }

    public string? TelegramUserName { get; set; }

    public decimal? TelegramChatId { get; set; }
    
    public short Status { get; set; }

    /// <summary>Tên hiển thị của người xem trên app (bình luận, trang cá nhân).</summary>
    public string? DisplayName { get; set; }
}
