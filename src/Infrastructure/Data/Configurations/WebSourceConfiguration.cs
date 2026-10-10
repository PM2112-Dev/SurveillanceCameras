using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Infrastructure.Data.Configurations;

public class WebSourceConfiguration : IEntityTypeConfiguration<WebSource>
{
    public void Configure(EntityTypeBuilder<WebSource> builder)
    {
        // Xoá tài khoản thì nguồn web chỉ mất liên kết cookie, không bị xoá theo.
        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.TytAccount)
            .WithMany()
            .HasForeignKey(x => x.TytAccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
