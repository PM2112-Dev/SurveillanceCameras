using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV22 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Đổi tên Story/Chapter → Work/Episode, giữ nguyên dữ liệu (EF mặc định sẽ xoá bảng rồi tạo lại).
            migrationBuilder.DropForeignKey(name: "FK_Chapters_Stories_StoryId", table: "Chapters");
            migrationBuilder.DropForeignKey(name: "FK_Chapters_Prompts_PromptId", table: "Chapters");
            migrationBuilder.DropForeignKey(name: "FK_Stories_StorySources_StorySourceId", table: "Stories");
            migrationBuilder.DropForeignKey(name: "FK_Stories_StoryVideoTypes_StoryVideoTypeId", table: "Stories");

            migrationBuilder.RenameTable(name: "Stories", newName: "Works");
            migrationBuilder.RenameTable(name: "Chapters", newName: "Episodes");
            migrationBuilder.Sql("ALTER TABLE \"Works\" RENAME CONSTRAINT \"PK_Stories\" TO \"PK_Works\";");
            migrationBuilder.Sql("ALTER TABLE \"Episodes\" RENAME CONSTRAINT \"PK_Chapters\" TO \"PK_Episodes\";");

            migrationBuilder.RenameColumn(name: "ChaptersSyncedAt", table: "Works", newName: "EpisodesSyncedAt");
            migrationBuilder.RenameColumn(name: "ChapterSyncError", table: "Works", newName: "EpisodeSyncError");
            migrationBuilder.RenameColumn(name: "StoryId", table: "Episodes", newName: "WorkId");
            migrationBuilder.RenameColumn(name: "ChapterNumber", table: "Episodes", newName: "EpisodeNumber");

            migrationBuilder.RenameIndex(name: "IX_Stories_StorySourceId", table: "Works", newName: "IX_Works_StorySourceId");
            migrationBuilder.RenameIndex(name: "IX_Stories_StoryVideoTypeId", table: "Works", newName: "IX_Works_StoryVideoTypeId");
            migrationBuilder.RenameIndex(name: "IX_Chapters_PromptId", table: "Episodes", newName: "IX_Episodes_PromptId");
            migrationBuilder.RenameIndex(name: "IX_Chapters_StoryId_ChapterNumber", table: "Episodes", newName: "IX_Episodes_WorkId_EpisodeNumber");
            migrationBuilder.RenameIndex(name: "IX_Chapters_StoryId_LinkRaw", table: "Episodes", newName: "IX_Episodes_WorkId_LinkRaw");
            migrationBuilder.RenameIndex(name: "IX_Chapters_StoryId_Status", table: "Episodes", newName: "IX_Episodes_WorkId_Status");

            // Phim không có nguồn truyện.
            migrationBuilder.AlterColumn<int>(name: "StorySourceId", table: "Works", type: "integer", nullable: true,
                oldClrType: typeof(int), oldType: "integer");

            // Tác phẩm đang có đều là truyện.
            migrationBuilder.AddColumn<int>(name: "Type", table: "Works", type: "integer", nullable: false, defaultValue: 1);
            migrationBuilder.AddColumn<int>(name: "UserProfileId", table: "Works", type: "integer", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ExternalId", table: "Works", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CoverUrl", table: "Works", type: "text", nullable: true);

            migrationBuilder.AddColumn<string>(name: "ExternalId", table: "Episodes", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "VideoUrl", table: "Episodes", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ThumbnailUrl", table: "Episodes", type: "text", nullable: true);
            migrationBuilder.AddColumn<int>(name: "DurationSeconds", table: "Episodes", type: "integer", nullable: true);

            migrationBuilder.CreateIndex(name: "IX_Works_Type", table: "Works", column: "Type");
            migrationBuilder.CreateIndex(name: "IX_Works_UserProfileId", table: "Works", column: "UserProfileId");
            migrationBuilder.CreateIndex(name: "IX_Works_CreatedBy_UserProfileId_ExternalId", table: "Works",
                columns: new[] { "CreatedBy", "UserProfileId", "ExternalId" }, unique: true, filter: "\"ExternalId\" IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_Episodes_WorkId_ExternalId", table: "Episodes",
                columns: new[] { "WorkId", "ExternalId" }, unique: true, filter: "\"ExternalId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(name: "FK_Works_StorySources_StorySourceId", table: "Works", column: "StorySourceId",
                principalTable: "StorySources", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Works_StoryVideoTypes_StoryVideoTypeId", table: "Works", column: "StoryVideoTypeId",
                principalTable: "StoryVideoTypes", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(name: "FK_Works_UserProfiles_UserProfileId", table: "Works", column: "UserProfileId",
                principalTable: "UserProfiles", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(name: "FK_Episodes_Works_WorkId", table: "Episodes", column: "WorkId",
                principalTable: "Works", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Episodes_Prompts_PromptId", table: "Episodes", column: "PromptId",
                principalTable: "Prompts", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("MigrationsV22 đổi tên bảng Stories/Chapters → Works/Episodes; không hỗ trợ quay lại.");
        }
    }
}
