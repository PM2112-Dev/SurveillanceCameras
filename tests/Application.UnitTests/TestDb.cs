using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Infrastructure.Data;
using SurveillanceCameras.Infrastructure.Data.Interceptors;

namespace SurveillanceCameras.Application.UnitTests;

/// <summary>DbContext in-memory có OutboxInterceptor thật, để kiểm tra sự kiện Kafka được ghi vào outbox.</summary>
public static class TestDb
{
    public static ApplicationDbContext Create()
    {
        var topics = new Mock<IKafkaTopicRegistry>();
        topics.Setup(x => x.GetTopic(It.IsAny<string>())).Returns<string>(type => type);

        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new OutboxInterceptor(topics.Object))
            .Options);
    }

    /// <summary>Loại sự kiện đang chờ trong outbox (không theo thứ tự).</summary>
    public static List<string> OutboxEventTypes(this ApplicationDbContext db)
        => db.OutboxMessages.AsNoTracking().Select(x => x.EventType).ToList();
}
