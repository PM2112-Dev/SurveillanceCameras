using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV18 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Prompts_PromptId",
                table: "Chapters");

            migrationBuilder.AddColumn<string>(
                name: "ChapterSyncError",
                table: "Stories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ChaptersSyncedAt",
                table: "Stories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PromptId",
                table: "Chapters",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EditedAt",
                table: "Chapters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "Chapters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FetchedAt",
                table: "Chapters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkRaw",
                table: "Chapters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Chapters",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Chương có sẵn (nhập tay): suy trạng thái từ nội dung (3 = Edited, 2 = Fetched, 1 = Listed).
            migrationBuilder.Sql("""
                UPDATE "Chapters" SET "Status" = CASE
                    WHEN COALESCE("ContentEdit", '') <> '' THEN 3
                    WHEN COALESCE("ContentRaw", '') <> '' THEN 2
                    ELSE 1 END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_StoryId_ChapterNumber",
                table: "Chapters",
                columns: new[] { "StoryId", "ChapterNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_StoryId_LinkRaw",
                table: "Chapters",
                columns: new[] { "StoryId", "LinkRaw" },
                unique: true,
                filter: "\"LinkRaw\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_StoryId_Status",
                table: "Chapters",
                columns: new[] { "StoryId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Prompts_PromptId",
                table: "Chapters",
                column: "PromptId",
                principalTable: "Prompts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Stories_StoryId",
                table: "Chapters",
                column: "StoryId",
                principalTable: "Stories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Prompts_PromptId",
                table: "Chapters");

            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Stories_StoryId",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_StoryId_ChapterNumber",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_StoryId_LinkRaw",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_StoryId_Status",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "ChapterSyncError",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "ChaptersSyncedAt",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "Error",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "FetchedAt",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "LinkRaw",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Chapters");

            migrationBuilder.AlterColumn<int>(
                name: "PromptId",
                table: "Chapters",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Prompts_PromptId",
                table: "Chapters",
                column: "PromptId",
                principalTable: "Prompts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
