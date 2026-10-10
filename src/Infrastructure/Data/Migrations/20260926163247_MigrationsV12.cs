using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "WebSources",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCookie",
                table: "WebSources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_WebSources_AccountId",
                table: "WebSources",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_WebSources_Accounts_AccountId",
                table: "WebSources",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WebSources_Accounts_AccountId",
                table: "WebSources");

            migrationBuilder.DropIndex(
                name: "IX_WebSources_AccountId",
                table: "WebSources");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "WebSources");

            migrationBuilder.DropColumn(
                name: "IsCookie",
                table: "WebSources");
        }
    }
}
