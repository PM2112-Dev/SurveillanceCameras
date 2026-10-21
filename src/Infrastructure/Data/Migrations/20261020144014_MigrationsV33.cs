using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV33 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TytRetryQueuedAt",
                table: "Works",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TytUploadBatchSize",
                table: "Works",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "TytUploadDailyTime",
                table: "Works",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TytUploadEveryHours",
                table: "Works",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TytUploadMode",
                table: "Works",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "QueuedAt",
                table: "Episodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TytUploadError",
                table: "Episodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TytUploadedAt",
                table: "Episodes",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TytRetryQueuedAt",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "TytUploadBatchSize",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "TytUploadDailyTime",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "TytUploadEveryHours",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "TytUploadMode",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "QueuedAt",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "TytUploadError",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "TytUploadedAt",
                table: "Episodes");
        }
    }
}
