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
                // Giờ gần nhất đã qua hôm nay: mỗi giờ hẹn là một lô, lô đó đã đăng nếu lần đăng gần nhất sau giờ hẹn.
                var scheduled = DailyTimes(dailyTime)
                    .Select(t => new DateTimeOffset(localNow.Date + t.ToTimeSpan(), localNow.Offset))
                    .Where(t => t <= localNow)
                    .OrderByDescending(t => t)
                    .Cast<DateTimeOffset?>()
                    .FirstOrDefault();
                if (scheduled is null) return false;
                return last is null || TimeZoneInfo.ConvertTime(last.Value, timeZone) < scheduled.Value;

            default:
                return false;
        }
    }

    /// <summary>Giờ đăng hợp lệ: "HH:mm" hoặc nhiều giờ cách nhau dấu phẩy ("08:00,12:00").</summary>
    public static bool IsValidDailyTime(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && value.Split(',', StringSplitOptions.TrimEntries).All(x => TimeOnly.TryParseExact(x, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _));

    /// <summary>Các giờ đăng mỗi ngày, tăng dần; chuỗi sai hoặc trống thì dùng giờ mặc định.</summary>
    public static List<TimeOnly> DailyTimes(string? value)
    {
        var times = IsValidDailyTime(value)
            ? value!.Split(',', StringSplitOptions.TrimEntries)
                .Select(x => TimeOnly.ParseExact(x, "HH:mm", CultureInfo.InvariantCulture)).Distinct().Order().ToList()
            : [];
        return times.Count > 0 ? times : [TimeOnly.ParseExact(DefaultDailyTime, "HH:mm", CultureInfo.InvariantCulture)];
    }
}
