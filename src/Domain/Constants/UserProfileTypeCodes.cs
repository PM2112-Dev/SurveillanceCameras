namespace SurveillanceCameras.Domain.Constants;

/// <summary>
/// Mã (<c>UserProfileType.Code</c>) cố định của từng nền tảng. Code dùng mã này để tìm đúng loại profile,
/// nên không đổi giá trị khi đã có dữ liệu. Thêm nền tảng mới (YouTube, Facebook…) thì bổ sung hằng số ở đây.
/// </summary>
public abstract class UserProfileTypeCodes
{
    public const string TikTok = "tiktok";
}
