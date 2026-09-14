using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CategoryStorySource_Category_CategoriesId",
                table: "CategoryStorySource");

            migrationBuilder.DropForeignKey(
                name: "FK_CategoryStorySource_StorySource_StorySourcesId",
                table: "CategoryStorySource");

            migrationBuilder.DropForeignKey(
                name: "FK_StorySource_WebSources_WebSourceId",
                table: "StorySource");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StorySource",
                table: "StorySource");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Category",
                table: "Category");

            migrationBuilder.RenameTable(
                name: "StorySource",
                newName: "StorySources");

            migrationBuilder.RenameTable(
                name: "Category",
                newName: "Categories");

            migrationBuilder.RenameIndex(
                name: "IX_StorySource_WebSourceId",
                table: "StorySources",
                newName: "IX_StorySources_WebSourceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StorySources",
                table: "StorySources",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryStorySource_Categories_CategoriesId",
                table: "CategoryStorySource",
                column: "CategoriesId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryStorySource_StorySources_StorySourcesId",
                table: "CategoryStorySource",
                column: "StorySourcesId",
                principalTable: "StorySources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StorySources_WebSources_WebSourceId",
                table: "StorySources",
                column: "WebSourceId",
                principalTable: "WebSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CategoryStorySource_Categories_CategoriesId",
                table: "CategoryStorySource");

            migrationBuilder.DropForeignKey(
                name: "FK_CategoryStorySource_StorySources_StorySourcesId",
                table: "CategoryStorySource");

            migrationBuilder.DropForeignKey(
                name: "FK_StorySources_WebSources_WebSourceId",
                table: "StorySources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StorySources",
                table: "StorySources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.RenameTable(
                name: "StorySources",
                newName: "StorySource");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "Category");

            migrationBuilder.RenameIndex(
                name: "IX_StorySources_WebSourceId",
                table: "StorySource",
                newName: "IX_StorySource_WebSourceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StorySource",
                table: "StorySource",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Category",
                table: "Category",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryStorySource_Category_CategoriesId",
                table: "CategoryStorySource",
                column: "CategoriesId",
                principalTable: "Category",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryStorySource_StorySource_StorySourcesId",
                table: "CategoryStorySource",
                column: "StorySourcesId",
                principalTable: "StorySource",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StorySource_WebSources_WebSourceId",
                table: "StorySource",
                column: "WebSourceId",
                principalTable: "WebSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
