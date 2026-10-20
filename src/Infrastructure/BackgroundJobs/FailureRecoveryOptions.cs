namespace SurveillanceCameras.Infrastructure.BackgroundJobs;

/// <summary>Tự xử lý lại tác vụ lỗi (mục <c>FailureRecovery</c> trong appsettings).</summary>
public class FailureRecoveryOptions
{
    public const string SectionName = "FailureRecovery";

    public bool Enabled { get; set; } = true;

    /// <summary>Chu kỳ kiểm tra (phút): mỗi chu kỳ kiểm tra nguyên nhân lỗi đã hết chưa rồi mới xử lý lại.</summary>
    public int IntervalMinutes { get; set; } = 10;

    /// <summary>Số tác vụ tối đa mỗi nhóm được đưa lại vào hàng đợi trong một chu kỳ.</summary>
    public int MaxPerGroup { get; set; } = 50;

    /// <summary>Tác vụ vừa vào hàng đợi chưa quá số phút này thì chưa đưa lại (hàng đợi có thể còn dài).</summary>
    public int RequeueAfterMinutes { get; set; } = 60;
}
