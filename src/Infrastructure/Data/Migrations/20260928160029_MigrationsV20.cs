using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TytAccountId",
                table: "WebSources",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TytError",
                table: "Stories",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebSources_TytAccountId",
                table: "WebSources",
                column: "TytAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_WebSources_Accounts_TytAccountId",
                table: "WebSources",
                column: "TytAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WebSources_Accounts_TytAccountId",
                table: "WebSources");

            migrationBuilder.DropIndex(
                name: "IX_WebSources_TytAccountId",
                table: "WebSources");

            migrationBuilder.DropColumn(
                name: "TytAccountId",
                table: "WebSources");

            migrationBuilder.DropColumn(
                name: "TytError",
                table: "Stories");
        }
    }
}
