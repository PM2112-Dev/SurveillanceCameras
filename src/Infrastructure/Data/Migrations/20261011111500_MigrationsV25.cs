using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV25 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StorageAccountId",
                table: "Episodes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StorageAttempts",
                table: "Episodes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StorageError",
                table: "Episodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "Episodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StorageSizeBytes",
                table: "Episodes",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StorageAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CloudflareAccountId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AccessKeyId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SecretAccessKey = table.Column<string>(type: "text", nullable: true),
                    Bucket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PublicBaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QuotaBytes = table.Column<long>(type: "bigint", nullable: false),
                    UsedBytes = table.Column<long>(type: "bigint", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    LastCheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_StorageAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_StorageAccountId",
                table: "Episodes",
                column: "StorageAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageAccounts_CreatedBy_IsEnabled_Priority",
                table: "StorageAccounts",
                columns: new[] { "CreatedBy", "IsEnabled", "Priority" });

            migrationBuilder.AddForeignKey(
                name: "FK_Episodes_StorageAccounts_StorageAccountId",
                table: "Episodes",
                column: "StorageAccountId",
                principalTable: "StorageAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Episodes_StorageAccounts_StorageAccountId",
                table: "Episodes");

            migrationBuilder.DropTable(
                name: "StorageAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Episodes_StorageAccountId",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "StorageAccountId",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "StorageAttempts",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "StorageError",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "StorageSizeBytes",
                table: "Episodes");
        }
    }
}
