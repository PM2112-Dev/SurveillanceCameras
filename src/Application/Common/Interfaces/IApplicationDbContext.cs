using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<WebSource> WebSources { get; }

    DbSet<StorySource> StorySources { get; }

    DbSet<Category> Categories { get; }

    DbSet<PromptType> PromptTypes { get; }

    DbSet<Prompt> Prompts { get; }

    DbSet<Work> Works { get; }

    DbSet<AccountType> AccountTypes { get; }

    DbSet<Account> Accounts { get; }

    DbSet<Episode> Episodes { get; }

    DbSet<AI> AIs { get; }

    DbSet<StoryVideoType> StoryVideoTypes { get; }

    DbSet<UserProfileType> UserProfileTypes { get; }

    DbSet<UserProfile> UserProfiles { get; }

    DbSet<Language> Languages { get; }

    DbSet<AppBookmark> AppBookmarks { get; }

    DbSet<AppWatchHistory> AppWatchHistories { get; }

    DbSet<AppComment> AppComments { get; }

    DbSet<AppEpisodeUnlock> AppEpisodeUnlocks { get; }

    DbSet<AppSubscription> AppSubscriptions { get; }

    DbSet<AppSubscriptionPlan> AppSubscriptionPlans { get; }

    DbSet<AppStoreEvent> AppStoreEvents { get; }

    DbSet<AppAdPlacement> AppAdPlacements { get; }

    DbSet<AppAdReward> AppAdRewards { get; }

    DbSet<AppAdScreen> AppAdScreens { get; }

    DbSet<AppAdStat> AppAdStats { get; }

    DbSet<AppMonetizationSettings> AppMonetizationSettings { get; }

    DbSet<StorageAccount> StorageAccounts { get; }
    
    /// <summary>Outbox pattern: pending integration events awaiting Kafka publish.</summary>
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Cộng <paramref name="delta"/> vào số lượt lưu của một phim ngay trong DB (không âm), an toàn khi nhiều người cùng lưu.</summary>
    Task AdjustBookmarkCountAsync(int workId, int delta, CancellationToken cancellationToken);

    /// <summary>Dùng cho tác vụ nền xử lý nhiều bản ghi: xoá bớt entity đang theo dõi giữa các lô.</summary>
    Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker ChangeTracker { get; }

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
