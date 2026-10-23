using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV37 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MonthlyVideoLimit",
                table: "StorageAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StorageAccountMonthlyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StorageAccountId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    VideosUploaded = table.Column<int>(type: "integer", nullable: false),
                    BytesUploaded = table.Column<long>(type: "bigint", nullable: false),
                    VideosDeleted = table.Column<int>(type: "integer", nullable: false),
                    BytesDeleted = table.Column<long>(type: "bigint", nullable: false),
                    PutObjectRequests = table.Column<long>(type: "bigint", nullable: false),
                    ListObjectsRequests = table.Column<long>(type: "bigint", nullable: false),
                    OtherClassARequests = table.Column<long>(type: "bigint", nullable: false),
                    GetObjectRequests = table.Column<long>(type: "bigint", nullable: false),
                    HeadObjectRequests = table.Column<long>(type: "bigint", nullable: false),
                    OtherClassBRequests = table.Column<long>(type: "bigint", nullable: false),
                    DeleteObjectRequests = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Code = table.Column<string>(type: "text", nullable: true),
                    BaseStatus = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageAccountMonthlyStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageAccountMonthlyStats_StorageAccounts_StorageAccountId",
                        column: x => x.StorageAccountId,
                        principalTable: "StorageAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorageAccountMonthlyStats_StorageAccountId_Year_Month",
                table: "StorageAccountMonthlyStats",
                columns: new[] { "StorageAccountId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorageAccountMonthlyStats");

            migrationBuilder.DropColumn(
                name: "MonthlyVideoLimit",
                table: "StorageAccounts");
        }
    }
}
