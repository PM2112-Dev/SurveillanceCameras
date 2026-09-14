using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<WebSource> WebSources { get; }
    
    DbSet<StorySource> StorySources { get; }

    DbSet<Category> Categories { get; }

    /// <summary>Outbox pattern: pending integration events awaiting Kafka publish.</summary>
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
