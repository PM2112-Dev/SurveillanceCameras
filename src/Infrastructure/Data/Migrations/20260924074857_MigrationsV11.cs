using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "UserProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FollowerCount",
                table: "UserProfiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FollowingCount",
                table: "UserProfiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "HeartCount",
                table: "UserProfiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastFetchedAt",
                table: "UserProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Signature",
                table: "UserProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Verified",
                table: "UserProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "VideoCount",
                table: "UserProfiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_CreatedBy_UserProfileTypeId_UserId",
                table: "UserProfiles",
                columns: new[] { "CreatedBy", "UserProfileTypeId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_CreatedBy_UserProfileTypeId_UserId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "FollowerCount",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "FollowingCount",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "HeartCount",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LastFetchedAt",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Signature",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Verified",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "VideoCount",
                table: "UserProfiles");
        }
    }
}
