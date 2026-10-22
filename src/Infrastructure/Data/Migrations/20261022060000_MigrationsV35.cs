using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV35 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TytApprovedAt",
                table: "Works",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TytPublishRequestedAt",
                table: "Works",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TytStage",
                table: "Works",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TytStageNote",
                table: "Works",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TytUploadDailyLimit",
                table: "Works",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Truyện đã có trên TYT từ trước coi như đã được duyệt: giữ nguyên việc đăng chương theo lịch như trước khi có bước duyệt.
            migrationBuilder.Sql("UPDATE \"Works\" SET \"TytStage\" = 4 WHERE \"TytStoryId\" IS NOT NULL AND \"TytStoryId\" <> '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TytApprovedAt",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "TytPublishRequestedAt",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "TytStage",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "TytStageNote",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "TytUploadDailyLimit",
                table: "Works");
        }
    }
}
