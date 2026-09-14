using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTotalChaptersToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StorySourceA_WebSources_WebSourceId",
                table: "StorySourceA");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StorySourceA",
                table: "StorySourceA");

            migrationBuilder.RenameTable(
                name: "StorySourceA",
                newName: "StorySourceAs");

            migrationBuilder.RenameIndex(
                name: "IX_StorySourceA_WebSourceId",
                table: "StorySourceAs",
                newName: "IX_StorySourceAs_WebSourceId");

            // Convert string to int using raw SQL with USING clause
            migrationBuilder.Sql("ALTER TABLE \"StorySourceAs\" ALTER COLUMN \"TotalChapters\" TYPE integer USING \"TotalChapters\"::integer;");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StorySourceAs",
                table: "StorySourceAs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StorySourceAs_WebSources_WebSourceId",
                table: "StorySourceAs",
                column: "WebSourceId",
                principalTable: "WebSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StorySourceAs_WebSources_WebSourceId",
                table: "StorySourceAs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StorySourceAs",
                table: "StorySourceAs");

            migrationBuilder.RenameTable(
                name: "StorySourceAs",
                newName: "StorySourceA");

            migrationBuilder.RenameIndex(
                name: "IX_StorySourceAs_WebSourceId",
                table: "StorySourceA",
                newName: "IX_StorySourceA_WebSourceId");

            migrationBuilder.AlterColumn<string>(
                name: "TotalChapters",
                table: "StorySourceA",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StorySourceA",
                table: "StorySourceA",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StorySourceA_WebSources_WebSourceId",
                table: "StorySourceA",
                column: "WebSourceId",
                principalTable: "WebSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
