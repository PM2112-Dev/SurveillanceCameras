namespace SurveillanceCameras.Application.Local.App;

/// <summary>Cấu hình cho app xem phim của người dùng cuối (mục <c>App</c> trong appsettings).</summary>
public class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Số tập đầu của mỗi phim ai cũng xem được.</summary>
    public int FreeEpisodes { get; set; } = 6;

    /// <summary>Số tập tiếp theo mở được bằng cách xem quảng cáo. Các tập sau đó chỉ dành cho VIP.</summary>
    public int AdEpisodes { get; set; } = 3;

    /// <summary>Link trang chính sách quyền riêng tư; để trống thì app hiện nội dung tạm.</summary>
    public string? PrivacyUrl { get; set; }

    /// <summary>Link trang điều khoản sử dụng; để trống thì app hiện nội dung tạm.</summary>
    public string? TermsUrl { get; set; }
}
