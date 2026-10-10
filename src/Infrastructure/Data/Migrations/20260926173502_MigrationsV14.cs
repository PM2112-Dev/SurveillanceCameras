using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseUrl",
                table: "AccountTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoginCookieName",
                table: "AccountTypes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseUrl",
                table: "AccountTypes");

            migrationBuilder.DropColumn(
                name: "LoginCookieName",
                table: "AccountTypes");
        }
    }
}
