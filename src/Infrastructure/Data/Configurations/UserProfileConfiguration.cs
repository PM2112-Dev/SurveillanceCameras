using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Infrastructure.Data.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        // Mỗi lần tra cứu kênh đều tìm profile đã lưu theo (người dùng, loại, UserId).
        builder.HasIndex(x => new { x.CreatedBy, x.UserProfileTypeId, x.UserId });
    }
}
