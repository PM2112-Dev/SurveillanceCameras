using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountTypeId = table.Column<int>(type: "integer", nullable: false),
                    Cookie = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                    table.PrimaryKey("PK_AccountTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                    table.PrimaryKey("PK_PromptTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StorySourceId = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    TytStoryId = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Author = table.Column<string>(type: "text", nullable: true),
                    IsFull = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    IsEarning = table.Column<bool>(type: "boolean", nullable: false),
                    IsComment = table.Column<bool>(type: "boolean", nullable: false),
                    ChapterUploaded = table.Column<int>(type: "integer", nullable: false),
                    EarnCount = table.Column<int>(type: "integer", nullable: false),
                    EarnNow = table.Column<int>(type: "integer", nullable: false),
                    AdCount = table.Column<int>(type: "integer", nullable: false),
                    Paid = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_Stories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Prompts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PromptTypeId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_Prompts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prompts_PromptTypes_PromptTypeId",
                        column: x => x.PromptTypeId,
                        principalTable: "PromptTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoryId = table.Column<int>(type: "integer", nullable: false),
                    PromptId = table.Column<int>(type: "integer", nullable: false),
                    TytChapterId = table.Column<string>(type: "text", nullable: true),
                    NameRaw = table.Column<string>(type: "text", nullable: true),
                    NameEdit = table.Column<string>(type: "text", nullable: true),
                    ChapterNumber = table.Column<int>(type: "integer", nullable: false),
                    ContentRaw = table.Column<string>(type: "text", nullable: true),
                    ContentEdit = table.Column<string>(type: "text", nullable: true),
                    IsUploaded = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_Chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chapters_Prompts_PromptId",
                        column: x => x.PromptId,
                        principalTable: "Prompts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_PromptId",
                table: "Chapters",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_PromptTypeId",
                table: "Prompts",
                column: "PromptTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "AccountTypes");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "Stories");

            migrationBuilder.DropTable(
                name: "Prompts");

            migrationBuilder.DropTable(
                name: "PromptTypes");
        }
    }
}
