using SurveillanceCameras.Infrastructure.Repository.AI;

namespace SurveillanceCameras.Infrastructure.IntegrationTests;

public class AIRetryDelayTests
{
    [Test]
    public void ParsesTheDelayGeminiReports()
    {
        Assert.That(AIErrorMessage.RetryDelay("quota. Please retry in 8h28m20.596693072s."),
            Is.EqualTo(TimeSpan.FromHours(8) + TimeSpan.FromMinutes(28) + TimeSpan.FromSeconds(20.596693072)).Within(TimeSpan.FromMilliseconds(1)));
        Assert.That(AIErrorMessage.RetryDelay("Please retry in 45s"), Is.EqualTo(TimeSpan.FromSeconds(45)));
        Assert.That(AIErrorMessage.RetryDelay("Please retry in 2m5.5s"), Is.EqualTo(TimeSpan.FromSeconds(125.5)));
        Assert.That(AIErrorMessage.RetryDelay("\"retryDelay\": \"30s\""), Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(AIErrorMessage.RetryDelay("lỗi khác"), Is.Null);
    }
}
