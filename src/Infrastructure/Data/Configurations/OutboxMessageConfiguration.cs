using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveillanceCameras.Domain.Common;

namespace SurveillanceCameras.Infrastructure.Data.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Topic).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(2000);

        // Index for efficient polling of unprocessed messages
        builder.HasIndex(x => x.ProcessedOn)
            .HasFilter("\"ProcessedOn\" IS NULL")
            .HasDatabaseName("IX_OutboxMessages_Unprocessed");
    }
}

