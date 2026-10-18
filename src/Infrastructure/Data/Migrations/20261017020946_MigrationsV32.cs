using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV32 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AdStatsEnabled",
                table: "AppMonetizationSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplaySeconds",
                table: "AppAdPlacements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EveryItems",
                table: "AppAdPlacements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "AppAdPlacements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ScreenCode",
                table: "AppAdPlacements",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Banner Khám phá có từ trước khi có màn hình: gắn vào màn Khám phá để hành vi cũ không đổi.
            migrationBuilder.Sql("UPDATE \"AppAdPlacements\" SET \"ScreenCode\" = 'home' WHERE \"Code\" = 'banner_home'");

            migrationBuilder.CreateTable(
                name: "AppAdScreens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseStatus = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAdScreens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppAdStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    PlacementCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScreenCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Platform = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Requests = table.Column<long>(type: "bigint", nullable: false),
                    Filled = table.Column<long>(type: "bigint", nullable: false),
                    Failed = table.Column<long>(type: "bigint", nullable: false),
                    Impressions = table.Column<long>(type: "bigint", nullable: false),
                    Clicks = table.Column<long>(type: "bigint", nullable: false),
                    Rewarded = table.Column<long>(type: "bigint", nullable: false),
                    Dismissed = table.Column<long>(type: "bigint", nullable: false),
                    RevenueMicros = table.Column<long>(type: "bigint", nullable: false),
                    LastError = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Code = table.Column<string>(type: "text", nullable: true),
                    BaseStatus = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAdStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppAdScreens_Code",
                table: "AppAdScreens",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppAdStats_Day",
                table: "AppAdStats",
                column: "Day");

            migrationBuilder.CreateIndex(
                name: "IX_AppAdStats_Day_PlacementCode_ScreenCode_Platform_Currency",
                table: "AppAdStats",
                columns: new[] { "Day", "PlacementCode", "ScreenCode", "Platform", "Currency" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppAdScreens");

            migrationBuilder.DropTable(
                name: "AppAdStats");

            migrationBuilder.DropColumn(
                name: "AdStatsEnabled",
                table: "AppMonetizationSettings");

            migrationBuilder.DropColumn(
                name: "DisplaySeconds",
                table: "AppAdPlacements");

            migrationBuilder.DropColumn(
                name: "EveryItems",
                table: "AppAdPlacements");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "AppAdPlacements");

            migrationBuilder.DropColumn(
                name: "ScreenCode",
                table: "AppAdPlacements");
        }
    }
}
