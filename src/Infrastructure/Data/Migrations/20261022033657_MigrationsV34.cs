using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV34 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "ExcludedEpisodeIds",
                table: "Works",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");

            migrationBuilder.AddColumn<bool>(
                name: "AutoSyncMovies",
                table: "UserProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "ExcludedDramaIds",
                table: "UserProfiles",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");

            migrationBuilder.AddColumn<string>(
                name: "MoviesSyncError",
                table: "UserProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MoviesSyncedAt",
                table: "UserProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "Episodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcludedEpisodeIds",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "AutoSyncMovies",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ExcludedDramaIds",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "MoviesSyncError",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "MoviesSyncedAt",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "Episodes");
        }
    }
}
