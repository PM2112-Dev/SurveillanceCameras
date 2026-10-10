using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Infrastructure.Data.Configurations;

public class StorySourceConfiguration : IEntityTypeConfiguration<StorySource>
{
    public void Configure(EntityTypeBuilder<StorySource> builder)
    {
        // Đồng bộ truyện từ trang nguồn và đánh dấu "đã đồng bộ" đều tra theo (người dùng, nguồn web, link).
        builder.HasIndex(x => new { x.CreatedBy, x.WebSourceId, x.LinkRaw });
    }
}
