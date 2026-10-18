using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Infrastructure.Data.Configurations;

// Dữ liệu của người xem trên app. Xoá phim/tập thì dữ liệu gắn với nó của người xem cũng xoá theo.

public class AppBookmarkConfiguration : IEntityTypeConfiguration<AppBookmark>
{
    public void Configure(EntityTypeBuilder<AppBookmark> builder)
    {
        builder.HasOne(x => x.Work).WithMany().HasForeignKey(x => x.WorkId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.CreatedBy, x.WorkId }).IsUnique();
        // Đếm lượt lưu của từng phim.
        builder.HasIndex(x => x.WorkId);
    }
}

public class AppWatchHistoryConfiguration : IEntityTypeConfiguration<AppWatchHistory>
{
    public void Configure(EntityTypeBuilder<AppWatchHistory> builder)
    {
        builder.HasOne(x => x.Work).WithMany().HasForeignKey(x => x.WorkId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.CreatedBy, x.WorkId }).IsUnique();
    }
}

public class AppCommentConfiguration : IEntityTypeConfiguration<AppComment>
{
    public const int MaxContentLength = 1000;

    public void Configure(EntityTypeBuilder<AppComment> builder)
    {
        builder.Property(x => x.Content).HasMaxLength(MaxContentLength).IsRequired();
        builder.Property(x => x.AuthorName).HasMaxLength(100);
        builder.HasOne(x => x.Work).WithMany().HasForeignKey(x => x.WorkId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.WorkId, x.Created });
        builder.HasIndex(x => x.CreatedBy);
    }
}

public class AppEpisodeUnlockConfiguration : IEntityTypeConfiguration<AppEpisodeUnlock>
{
    public void Configure(EntityTypeBuilder<AppEpisodeUnlock> builder)
    {
        builder.HasOne(x => x.Episode).WithMany().HasForeignKey(x => x.EpisodeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.CreatedBy, x.EpisodeId }).IsUnique();
    }
}

public class AppSubscriptionConfiguration : IEntityTypeConfiguration<AppSubscription>
{
    public void Configure(EntityTypeBuilder<AppSubscription> builder)
    {
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Plan).HasMaxLength(50);
        builder.Property(x => x.ProductId).HasMaxLength(200);
        builder.Property(x => x.ExternalId).HasMaxLength(512);
        builder.Property(x => x.PreviousExternalId).HasMaxLength(512);
        builder.HasIndex(x => new { x.UserId, x.ExpiresAt });
        // Mỗi người một gói do quản trị viên cấp; mỗi gói của cửa hàng chỉ gắn với một dòng.
        builder.HasIndex(x => x.UserId).IsUnique().HasFilter($"\"Source\" = {(int)SubscriptionSource.Admin}");
        builder.HasIndex(x => new { x.Source, x.ExternalId }).IsUnique().HasFilter("\"ExternalId\" IS NOT NULL");
        builder.HasIndex(x => x.PreviousExternalId);
    }
}

public class AppSubscriptionPlanConfiguration : IEntityTypeConfiguration<AppSubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<AppSubscriptionPlan> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(200);
        builder.Property(x => x.AppleProductId).HasMaxLength(200);
        builder.Property(x => x.GoogleProductId).HasMaxLength(200);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class AppStoreEventConfiguration : IEntityTypeConfiguration<AppStoreEvent>
{
    public void Configure(EntityTypeBuilder<AppStoreEvent> builder)
    {
        builder.Property(x => x.NotificationId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(100);
        builder.Property(x => x.ExternalId).HasMaxLength(512);
        builder.Property(x => x.Error).HasMaxLength(1000);
        builder.HasIndex(x => new { x.Store, x.NotificationId }).IsUnique();
    }
}

public class AppAdPlacementConfiguration : IEntityTypeConfiguration<AppAdPlacement>
{
    public void Configure(EntityTypeBuilder<AppAdPlacement> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(100);
        builder.Property(x => x.AndroidUnitId).HasMaxLength(100);
        builder.Property(x => x.IosUnitId).HasMaxLength(100);
        builder.Property(x => x.ScreenCode).HasMaxLength(50);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class AppAdScreenConfiguration : IEntityTypeConfiguration<AppAdScreen>
{
    public void Configure(EntityTypeBuilder<AppAdScreen> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class AppAdStatConfiguration : IEntityTypeConfiguration<AppAdStat>
{
    public void Configure(EntityTypeBuilder<AppAdStat> builder)
    {
        builder.Property(x => x.PlacementCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ScreenCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Platform).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(200);
        builder.HasIndex(x => new { x.Day, x.PlacementCode, x.ScreenCode, x.Platform, x.Currency }).IsUnique();
        builder.HasIndex(x => x.Day);
    }
}

public class AppAdRewardConfiguration : IEntityTypeConfiguration<AppAdReward>
{
    public void Configure(EntityTypeBuilder<AppAdReward> builder)
    {
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.TransactionId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AdUnit).HasMaxLength(100);
        builder.HasIndex(x => x.TransactionId).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.EpisodeId, x.ConsumedAt });
    }
}

public class AppMonetizationSettingsConfiguration : IEntityTypeConfiguration<AppMonetizationSettings>
{
    public void Configure(EntityTypeBuilder<AppMonetizationSettings> builder)
    {
        builder.Property(x => x.PaywallVariant).HasMaxLength(10).IsRequired();
        builder.Property(x => x.MinAppVersion).HasMaxLength(20);
    }
}
