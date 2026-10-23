using System.Reflection;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Common;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SurveillanceCameras.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<WebSource> WebSources => Set<WebSource>();
    
    public DbSet<StorySource> StorySources => Set<StorySource>();

    public DbSet<Category> Categories => Set<Category>();
    
    public DbSet<PromptType> PromptTypes => Set<PromptType>();
    
    public DbSet<Prompt> Prompts => Set<Prompt>();
    
    public DbSet<Work> Works => Set<Work>();
    
    public DbSet<AccountType> AccountTypes => Set<AccountType>();
    
    public DbSet<Account> Accounts => Set<Account>();
    
    public DbSet<Episode> Episodes => Set<Episode>();
    
    public DbSet<AI> AIs => Set<AI>();

    public DbSet<StoryVideoType> StoryVideoTypes => Set<StoryVideoType>();

    public DbSet<UserProfileType> UserProfileTypes => Set<UserProfileType>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<Language> Languages => Set<Language>();

    public DbSet<AppBookmark> AppBookmarks => Set<AppBookmark>();

    public DbSet<AppWatchHistory> AppWatchHistories => Set<AppWatchHistory>();

    public DbSet<AppComment> AppComments => Set<AppComment>();

    public DbSet<AppEpisodeUnlock> AppEpisodeUnlocks => Set<AppEpisodeUnlock>();

    public DbSet<AppSubscription> AppSubscriptions => Set<AppSubscription>();

    public DbSet<AppSubscriptionPlan> AppSubscriptionPlans => Set<AppSubscriptionPlan>();

    public DbSet<AppStoreEvent> AppStoreEvents => Set<AppStoreEvent>();

    public DbSet<AppAdPlacement> AppAdPlacements => Set<AppAdPlacement>();

    public DbSet<AppAdReward> AppAdRewards => Set<AppAdReward>();

    public DbSet<AppAdScreen> AppAdScreens => Set<AppAdScreen>();

    public DbSet<AppAdStat> AppAdStats => Set<AppAdStat>();

    public DbSet<AppMonetizationSettings> AppMonetizationSettings => Set<AppMonetizationSettings>();

    public DbSet<StorageAccount> StorageAccounts => Set<StorageAccount>();

    public DbSet<StorageAccountMonthlyStat> StorageAccountMonthlyStats => Set<StorageAccountMonthlyStat>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public async Task AdjustBookmarkCountAsync(int workId, int delta, CancellationToken cancellationToken)
    {
        if (delta == 0) return;

        if (Database.IsRelational())
        {
            // Một câu UPDATE: hai người lưu cùng lúc không đè mất lượt của nhau.
            await Works.Where(x => x.Id == workId).ExecuteUpdateAsync(s => s.SetProperty(
                x => x.BookmarkCount, x => x.BookmarkCount + delta < 0 ? 0 : x.BookmarkCount + delta), cancellationToken);
            return;
        }

        // Provider in-memory (unit test) không hỗ trợ ExecuteUpdate.
        var work = await Works.FindAsync([workId], cancellationToken);
        if (work is null) return;
        work.BookmarkCount = Math.Max(0, work.BookmarkCount + delta);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Npgsql only accepts a UTC offset when writing to 'timestamp with time zone'. A client sending
        // a local offset (e.g. +07:00) would otherwise fail with ArgumentException at SaveChanges.
        // Converting on the way in preserves the instant; timestamptz stores UTC regardless.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<UtcDateTimeOffsetConverter>();
    }

    private sealed class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
    {
        public UtcDateTimeOffsetConverter()
            : base(v => v.ToUniversalTime(), v => v)
        {
        }
    }
}
