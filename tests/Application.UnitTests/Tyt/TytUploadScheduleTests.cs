using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Local.Episodes;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.UnitTests.Tyt;

public class TytUploadScheduleTests
{
    private static readonly TimeZoneInfo Vn = TimeZoneInfo.CreateCustomTimeZone("vn", TimeSpan.FromHours(7), "vn", "vn");

    private static DateTimeOffset Utc(int day, int hour, int minute = 0) => new(2026, 10, day, hour, minute, 0, TimeSpan.Zero);

    [Test]
    public void Manual_NeverDue_Immediate_AlwaysDue()
    {
        TytUploadSchedule.IsDue(TytUploadMode.Manual, 1, null, null, Utc(20, 1), Vn).ShouldBeFalse();
        TytUploadSchedule.IsDue(TytUploadMode.Immediate, 1, null, Utc(20, 1), Utc(20, 1), Vn).ShouldBeTrue();
    }

    [Test]
    public void Hourly_DueWhenNeverUploaded_OrAfterTheInterval()
    {
        TytUploadSchedule.IsDue(TytUploadMode.Hourly, 2, null, null, Utc(20, 1), Vn).ShouldBeTrue();
        TytUploadSchedule.IsDue(TytUploadMode.Hourly, 2, null, Utc(20, 1), Utc(20, 2, 59), Vn).ShouldBeFalse();
        TytUploadSchedule.IsDue(TytUploadMode.Hourly, 2, null, Utc(20, 1), Utc(20, 3), Vn).ShouldBeTrue();
    }

    [Test]
    public void Daily_DueOncePerDay_OnceTheScheduledTimePasses()
    {
        // 08:30 giờ VN = 01:30 UTC.
        TytUploadSchedule.IsDue(TytUploadMode.Daily, 1, "08:30", null, Utc(20, 1, 29), Vn).ShouldBeFalse();
        TytUploadSchedule.IsDue(TytUploadMode.Daily, 1, "08:30", null, Utc(20, 1, 30), Vn).ShouldBeTrue();

        // Đã đăng sau giờ hẹn hôm nay → hôm nay không đăng nữa, sang ngày sau mới tới lượt.
        TytUploadSchedule.IsDue(TytUploadMode.Daily, 1, "08:30", Utc(20, 1, 31), Utc(20, 10), Vn).ShouldBeFalse();
        TytUploadSchedule.IsDue(TytUploadMode.Daily, 1, "08:30", Utc(20, 1, 31), Utc(21, 1, 30), Vn).ShouldBeTrue();
    }

    [Test]
    public void Daily_InvalidTime_FallsBackToTheDefault()
    {
        TytUploadSchedule.IsValidDailyTime("25:99").ShouldBeFalse();
        TytUploadSchedule.IsValidDailyTime("08:30").ShouldBeTrue();
        // Mặc định 08:00 VN = 01:00 UTC.
        TytUploadSchedule.IsDue(TytUploadMode.Daily, 1, "bậy", null, Utc(20, 0, 59), Vn).ShouldBeFalse();
        TytUploadSchedule.IsDue(TytUploadMode.Daily, 1, "bậy", null, Utc(20, 1), Vn).ShouldBeTrue();
    }

    [Test]
    public void Daily_WithSeveralTimes_EachTimeIsItsOwnBatch()
    {
        // 08:00, 12:00, 20:00 giờ VN = 01:00, 05:00, 13:00 UTC.
        const string times = "20:00, 08:00,12:00";
        TytUploadSchedule.DailyTimes(times).Select(x => x.ToString("HH:mm")).ShouldBe(["08:00", "12:00", "20:00"]);
        TytUploadSchedule.IsValidDailyTime(times).ShouldBeTrue();
        TytUploadSchedule.IsValidDailyTime("08:00,25:00").ShouldBeFalse();

        // Đã đăng lúc 08:05 → 08:30 chưa tới lô mới, 12:00 tới lô mới.
        TytUploadSchedule.IsDue(TytUploadMode.Daily, 1, times, Utc(20, 1, 5), Utc(20, 2), Vn).ShouldBeFalse();
        TytUploadSchedule.IsDue(TytUploadMode.Daily, 1, times, Utc(20, 1, 5), Utc(20, 5), Vn).ShouldBeTrue();
        // Sau lô 12:00 đã đăng, tới 20:00 mới có lô kế.
        TytUploadSchedule.IsDue(TytUploadMode.Daily, 1, times, Utc(20, 5, 2), Utc(20, 12), Vn).ShouldBeFalse();
        TytUploadSchedule.IsDue(TytUploadMode.Daily, 1, times, Utc(20, 5, 2), Utc(20, 13), Vn).ShouldBeTrue();
    }
}
