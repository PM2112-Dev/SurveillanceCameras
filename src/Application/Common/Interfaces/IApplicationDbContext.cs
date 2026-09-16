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

    DbSet<AI> AIs { get; }

    /// <summary>Outbox pattern: pending integration events awaiting Kafka publish.</summary>
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Runs <paramref name="operation"/> inside one DB transaction via EF Core's execution
    /// strategy (so it composes correctly whether or not retry-on-failure is enabled — Aspire's
    /// EnrichNpgsqlDbContext enables it by default, and a raw Database.BeginTransactionAsync call
    /// would throw under a retrying strategy). Use this when a handler needs a DB-generated Id
    /// (e.g. an identity column) before it can raise an integration event that carries that Id —
    /// call SaveChangesAsync once to get the Id, raise the event, then SaveChangesAsync again so
    /// OutboxInterceptor writes the OutboxMessage row in the SAME transaction as the insert.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
