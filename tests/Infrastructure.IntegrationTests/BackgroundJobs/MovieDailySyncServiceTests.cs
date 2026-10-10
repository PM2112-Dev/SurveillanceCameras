using SurveillanceCameras.Infrastructure.BackgroundJobs;

namespace SurveillanceCameras.Infrastructure.IntegrationTests.BackgroundJobs;

public class MovieDailySyncServiceTests
{
    private static readonly TimeSpan Vn = TimeSpan.FromHours(7);

    [Test]
    public void BeforeRunTime_RunsToday()
        => Assert.That(MovieDailySyncService.NextRunDelay(new DateTimeOffset(2026, 9, 30, 1, 30, 0, Vn), new TimeOnly(3, 0)),
            Is.EqualTo(TimeSpan.FromMinutes(90)));

    [Test]
    public void AfterOrAtRunTime_RunsTomorrow()
    {
        Assert.That(MovieDailySyncService.NextRunDelay(new DateTimeOffset(2026, 9, 30, 3, 0, 0, Vn), new TimeOnly(3, 0)),
            Is.EqualTo(TimeSpan.FromDays(1)));
        Assert.That(MovieDailySyncService.NextRunDelay(new DateTimeOffset(2026, 9, 30, 23, 0, 0, Vn), new TimeOnly(3, 0)),
            Is.EqualTo(TimeSpan.FromHours(4)));
    }
}
