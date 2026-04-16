using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "TodoLists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TodoLists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "TodoItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TodoItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Areas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Areas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Cameras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AreaId = table.Column<int>(type: "integer", nullable: false),
                    CameraType = table.Column<short>(type: "smallint", nullable: true),
                    CameraLink = table.Column<string>(type: "text", nullable: true),
                    LanIpAddress = table.Column<string>(type: "text", nullable: true),
                    WabIpAddress = table.Column<string>(type: "text", nullable: true),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    IntegratedCamId = table.Column<int>(type: "integer", nullable: true),
                    DeviceStatus = table.Column<short>(type: "smallint", nullable: false),
                    Brand = table.Column<short>(type: "smallint", nullable: false),
                    CameraUserName = table.Column<string>(type: "text", nullable: true),
                    CameraPassword = table.Column<string>(type: "text", nullable: true),
                    PtzType = table.Column<short>(type: "smallint", nullable: false),
                    Onvif = table.Column<bool>(type: "boolean", nullable: false),
                    TourId = table.Column<int>(type: "integer", nullable: false),
                    FireDetection = table.Column<bool>(type: "boolean", nullable: false),
                    SmokeDetection = table.Column<bool>(type: "boolean", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cameras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TitleRaw = table.Column<string>(type: "text", nullable: true),
                    StoryWebId = table.Column<string>(type: "text", nullable: true),
                    LinkRaw = table.Column<string>(type: "text", nullable: true),
                    Author = table.Column<string>(type: "text", nullable: true),
                    TotalChapters = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    DescriptionRaw = table.Column<string>(type: "text", nullable: true),
                    DescriptionEdit = table.Column<string>(type: "text", nullable: true),
                    LinkChapterOne = table.Column<string>(type: "text", nullable: true),
                    Genres = table.Column<List<string>>(type: "text[]", nullable: true),
                    IsScraped = table.Column<bool>(type: "boolean", nullable: false),
                    IsEdited = table.Column<bool>(type: "boolean", nullable: false),
                    IsComment = table.Column<bool>(type: "boolean", nullable: false),
                    Publish = table.Column<int>(type: "integer", nullable: false),
                    Uploaded = table.Column<int>(type: "integer", nullable: false),
                    IsFull = table.Column<bool>(type: "boolean", nullable: false),
                    EarnCount = table.Column<int>(type: "integer", nullable: false),
                    EarnNow = table.Column<int>(type: "integer", nullable: false),
                    AdCount = table.Column<int>(type: "integer", nullable: false),
                    Paid = table.Column<int>(type: "integer", nullable: false),
                    IsEarning = table.Column<bool>(type: "boolean", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stories", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cameras");

            migrationBuilder.DropTable(
                name: "Stories");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "TodoLists");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TodoLists");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Areas");
        }
    }
}
