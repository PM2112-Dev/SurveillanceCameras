using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV36 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TytEarningNote",
                table: "Works",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TytEarningSyncedAt",
                table: "Works",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TytEarningNote",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "TytEarningSyncedAt",
                table: "Works");
        }
    }
}
