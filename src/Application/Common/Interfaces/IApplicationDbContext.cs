using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<WebSource> WebSources { get; }

    DbSet<StorySource> StorySources { get; }

    DbSet<Category> Categories { get; }

    DbSet<PromptType> PromptTypes { get; }

    DbSet<Prompt> Prompts { get; }
    
    DbSet<Story> Stories { get; }
    
    DbSet<AccountType> AccountTypes { get; }
    
    DbSet<Account> Accounts { get; }
    
    DbSet<Chapter> Chapters { get; }

    /// <summary>Outbox pattern: pending integration events awaiting Kafka publish.</summary>
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
