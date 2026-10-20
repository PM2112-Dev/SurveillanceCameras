using System.Globalization;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Episodes;

/// <summary>Tính xem truyện đã tới lượt đăng lô chương kế tiếp lên TYT chưa (thuần tính toán, không đụng DB).</summary>
public static class TytUploadSchedule
{
    public const string DefaultDailyTime = "08:00";

    /// <param name="last">Mốc đăng/đưa vào hàng đợi gần nhất của truyện; <c>null</c> là chưa đăng lần nào.</param>
    /// <param name="timeZone">Múi giờ để hiểu giờ đăng mỗi ngày.</param>
    public static bool IsDue(TytUploadMode mode, int everyHours, string? dailyTime, DateTimeOffset? last,
        DateTimeOffset now, TimeZoneInfo timeZone)
    {
        switch (mode)
        {
            case TytUploadMode.Immediate:
                return true;

            case TytUploadMode.Hourly:
                return last is null || now - last.Value >= TimeSpan.FromHours(Math.Max(1, everyHours));

            case TytUploadMode.Daily:
                var localNow = TimeZoneInfo.ConvertTime(now, timeZone);
                var scheduled = ScheduledToday(localNow, dailyTime);
                if (localNow < scheduled) return false;
                return last is null || TimeZoneInfo.ConvertTime(last.Value, timeZone) < scheduled;

            default:
                return false;
        }
    }

    /// <summary>Giờ đăng hợp lệ ("HH:mm"); sai định dạng thì dùng mặc định.</summary>
    public static bool IsValidDailyTime(string? value)
        => TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static DateTimeOffset ScheduledToday(DateTimeOffset localNow, string? dailyTime)
    {
        var time = TimeOnly.TryParseExact(dailyTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : TimeOnly.ParseExact(DefaultDailyTime, "HH:mm", CultureInfo.InvariantCulture);
        return new DateTimeOffset(localNow.Date + time.ToTimeSpan(), localNow.Offset);
    }
}
