using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV27 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdFromEpisode",
                table: "Works",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdToEpisode",
                table: "Works",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasCustomAccess",
                table: "Works",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "VipFromEpisode",
                table: "Works",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VipToEpisode",
                table: "Works",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdFromEpisode",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "AdToEpisode",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "HasCustomAccess",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "VipFromEpisode",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "VipToEpisode",
                table: "Works");
        }
    }
}
