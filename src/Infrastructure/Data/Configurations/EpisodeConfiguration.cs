using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Infrastructure.Data.Configurations;

public class EpisodeConfiguration : IEntityTypeConfiguration<Episode>
{
    public void Configure(EntityTypeBuilder<Episode> builder)
    {
        builder.HasOne(x => x.Work)
            .WithMany(x => x.Episodes)
            .HasForeignKey(x => x.WorkId)
            .OnDelete(DeleteBehavior.Cascade);

        // Xoá prompt không được xoá theo các chương đã biên tập bằng nó.
        builder.HasOne(x => x.Prompt)
            .WithMany()
            .HasForeignKey(x => x.PromptId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Language)
            .WithMany()
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.SetNull);

        // Còn tập đang lưu video ở tài khoản thì không cho xoá tài khoản lưu trữ (link phát sẽ chết).
        builder.HasOne(x => x.StorageAccount)
            .WithMany()
            .HasForeignKey(x => x.StorageAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Đồng bộ lại mục lục nhận ra chương đã có theo link (chương nhập tay không có link).
        builder.HasIndex(x => new { x.WorkId, x.LinkRaw })
            .IsUnique()
            .HasFilter("\"LinkRaw\" IS NOT NULL");

        // Đồng bộ lại tập phim nhận ra tập đã có theo ID trên nền tảng nguồn.
        builder.HasIndex(x => new { x.WorkId, x.ExternalId })
            .IsUnique()
            .HasFilter("\"ExternalId\" IS NOT NULL");

        // Danh sách chương/tập theo thứ tự và thống kê theo trạng thái trong một tác phẩm.
        builder.HasIndex(x => new { x.WorkId, x.EpisodeNumber });
        builder.HasIndex(x => new { x.WorkId, x.Status });
    }
}
