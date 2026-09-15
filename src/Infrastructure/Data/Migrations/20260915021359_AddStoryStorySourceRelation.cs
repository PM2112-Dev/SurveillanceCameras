using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryStorySourceRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Stories_StorySourceId",
                table: "Stories",
                column: "StorySourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_StorySources_StorySourceId",
                table: "Stories",
                column: "StorySourceId",
                principalTable: "StorySources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stories_StorySources_StorySourceId",
                table: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_Stories_StorySourceId",
                table: "Stories");
        }
    }
}
