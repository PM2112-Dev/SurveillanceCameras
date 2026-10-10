using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV16 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PromptTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "AIs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Project",
                table: "AIs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "AIs",
                type: "integer",
                nullable: false,
                // AI đã có trước đây đều là key Gemini (AIProvider.Gemini = 1); 0 không phải nền tảng hợp lệ.
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "PromptTypes");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "AIs");

            migrationBuilder.DropColumn(
                name: "Project",
                table: "AIs");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "AIs");
        }
    }
}
