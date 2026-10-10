using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StorySources_CreatedBy_WebSourceId_LinkRaw",
                table: "StorySources",
                columns: new[] { "CreatedBy", "WebSourceId", "LinkRaw" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StorySources_CreatedBy_WebSourceId_LinkRaw",
                table: "StorySources");
        }
    }
}
