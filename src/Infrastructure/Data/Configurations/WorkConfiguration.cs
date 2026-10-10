using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Infrastructure.Data.Configurations;

public class WorkConfiguration : IEntityTypeConfiguration<Work>
{
    public void Configure(EntityTypeBuilder<Work> builder)
    {
        builder.Property(x => x.Type).HasDefaultValue(WorkType.Story);

        // Xoá loại video thì tác phẩm chỉ mất liên kết, không bị chặn/xoá theo.
        builder.HasOne(x => x.StoryVideoType)
            .WithMany(x => x.Works)
            .HasForeignKey(x => x.StoryVideoTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Như trước khi đổi thành Work: xoá nguồn truyện thì xoá truyện tạo từ nó.
        builder.HasOne(x => x.StorySource)
            .WithMany(x => x.Works)
            .HasForeignKey(x => x.StorySourceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Xoá kênh thì phim chỉ mất liên kết kênh nguồn.
        builder.HasOne(x => x.UserProfile)
            .WithMany()
            .HasForeignKey(x => x.UserProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        // Một drama của một kênh chỉ tạo một phim cho mỗi người dùng.
        builder.HasIndex(x => new { x.CreatedBy, x.UserProfileId, x.ExternalId })
            .IsUnique()
            .HasFilter("\"ExternalId\" IS NOT NULL");

        builder.HasIndex(x => x.Type);

        // Danh sách phim xếp theo lượt xem.
        builder.HasIndex(x => new { x.CreatedBy, x.Type, x.ViewCount });
    }
}
