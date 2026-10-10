using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveillanceCameras.Domain.Entities;

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
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
