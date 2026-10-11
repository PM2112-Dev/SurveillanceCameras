using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Infrastructure.Data.Configurations;

public class StorageAccountConfiguration : IEntityTypeConfiguration<StorageAccount>
{
    public void Configure(EntityTypeBuilder<StorageAccount> builder)
    {
        builder.Property(x => x.CloudflareAccountId).HasMaxLength(100);
        builder.Property(x => x.AccessKeyId).HasMaxLength(200);
        builder.Property(x => x.Bucket).HasMaxLength(100);
        builder.Property(x => x.PublicBaseUrl).HasMaxLength(500);
        builder.Ignore(x => x.FreeBytes);

        // Chọn tài khoản để lưu: của một người dùng, đang bật, theo thứ tự ưu tiên.
        builder.HasIndex(x => new { x.CreatedBy, x.IsEnabled, x.Priority });
    }
}
