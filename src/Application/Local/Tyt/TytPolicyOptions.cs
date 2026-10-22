namespace SurveillanceCameras.Application.Local.Tyt;

/// <summary>Quy tắc đưa truyện lên TYT (mục <c>TytUpload</c> trong appsettings, dùng chung với tác vụ đăng chương).</summary>
public class TytPolicyOptions
{
    /// <summary>Số truyện đang đăng (chưa hoàn thành) tối đa của một tài khoản TYT cùng lúc. Truyện hoàn thành không tính, nên bổ sung thêm được.</summary>
    public int MaxActiveStoriesPerAccount { get; set; } = 20;

    /// <summary>Số chương đầu đăng trước khi gửi yêu cầu công khai và chờ TYT duyệt.</summary>
    public int FirstChaptersToPublish { get; set; } = 3;

    /// <summary>Múi giờ để hiểu giờ đăng và "ngày" của hạn mức chương mỗi ngày (IANA).</summary>
    public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";

    /// <summary>Gửi lại yêu cầu công khai / kiểm tra lại cách nhau tối thiểu bấy nhiêu phút khi TYT báo lỗi.</summary>
    public int RetryMinutes { get; set; } = 10;
}
