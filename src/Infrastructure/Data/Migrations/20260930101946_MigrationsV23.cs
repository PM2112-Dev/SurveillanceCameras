using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV23 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ViewCount",
                table: "Works",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Works_CreatedBy_Type_ViewCount",
                table: "Works",
                columns: new[] { "CreatedBy", "Type", "ViewCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Works_CreatedBy_Type_ViewCount",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Works");
        }
    }
}
