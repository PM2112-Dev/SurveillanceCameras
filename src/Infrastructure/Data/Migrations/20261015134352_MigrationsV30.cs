using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV30 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookmarkCount",
                table: "Works",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Tính lượt lưu cho các phim đã có người lưu trước khi có cột này.
            migrationBuilder.Sql("""
                UPDATE "Works" w SET "BookmarkCount" = b.n
                FROM (SELECT "WorkId", COUNT(*) AS n FROM "AppBookmarks" GROUP BY "WorkId") b
                WHERE b."WorkId" = w."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookmarkCount",
                table: "Works");
        }
    }
}
