using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV28 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hạn mức trước đây được nhập theo GB nhưng lưu với 1 GB = 1024³ byte, trong khi Cloudflare tính 1 GB = 1 tỷ byte
            // ("9,5 GB" thành 10,2 GB trên R2). Đổi lại để giữ đúng con số GB người dùng đã nhập.
            migrationBuilder.Sql(
                """
                UPDATE "StorageAccounts"
                SET "QuotaBytes" = ROUND("QuotaBytes" / 1073741824.0 * 1000000000.0)::bigint;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "StorageAccounts"
                SET "QuotaBytes" = ROUND("QuotaBytes" / 1000000000.0 * 1073741824.0)::bigint;
                """);
        }
    }
}
