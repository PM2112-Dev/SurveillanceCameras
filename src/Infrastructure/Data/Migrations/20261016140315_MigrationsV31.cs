using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SurveillanceCameras.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrationsV31 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppSubscriptions_UserId",
                table: "AppSubscriptions");

            migrationBuilder.AddColumn<bool>(
                name: "AutoRenew",
                table: "AppSubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "AppSubscriptions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSandbox",
                table: "AppSubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreviousExternalId",
                table: "AppSubscriptions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductId",
                table: "AppSubscriptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // Gói đã có trước đây đều do quản trị viên cấp (SubscriptionSource.Admin).
            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "AppSubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // SubscriptionStatus.Active
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AppSubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VerifiedAt",
                table: "AppSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppAdPlacements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    AndroidUnitId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IosUnitId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DailyLimit = table.Column<int>(type: "integer", nullable: false),
                    CooldownSeconds = table.Column<int>(type: "integer", nullable: false),
                    EveryEpisodes = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseStatus = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAdPlacements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppAdRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    EpisodeId = table.Column<int>(type: "integer", nullable: true),
                    TransactionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AdUnit = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RewardedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_AppAdRewards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppMonetizationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PurchasesEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RequireAdVerification = table.Column<bool>(type: "boolean", nullable: false),
                    PaywallVariant = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PaywallAfterRewardedAds = table.Column<int>(type: "integer", nullable: false),
                    MinAppVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_AppMonetizationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppStoreEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Store = table.Column<int>(type: "integer", nullable: false),
                    NotificationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_AppStoreEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppleProductId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GoogleProductId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PeriodDays = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TrialDays = table.Column<int>(type: "integer", nullable: false),
                    IsHighlighted = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseStatus = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSubscriptions_PreviousExternalId",
                table: "AppSubscriptions",
                column: "PreviousExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSubscriptions_Source_ExternalId",
                table: "AppSubscriptions",
                columns: new[] { "Source", "ExternalId" },
                unique: true,
                filter: "\"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppSubscriptions_UserId",
                table: "AppSubscriptions",
                column: "UserId",
                unique: true,
                filter: "\"Source\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AppSubscriptions_UserId_ExpiresAt",
                table: "AppSubscriptions",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppAdPlacements_Code",
                table: "AppAdPlacements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppAdRewards_TransactionId",
                table: "AppAdRewards",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppAdRewards_UserId_EpisodeId_ConsumedAt",
                table: "AppAdRewards",
                columns: new[] { "UserId", "EpisodeId", "ConsumedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppStoreEvents_Store_NotificationId",
                table: "AppStoreEvents",
                columns: new[] { "Store", "NotificationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSubscriptionPlans_Code",
                table: "AppSubscriptionPlans",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppAdPlacements");

            migrationBuilder.DropTable(
                name: "AppAdRewards");

            migrationBuilder.DropTable(
                name: "AppMonetizationSettings");

            migrationBuilder.DropTable(
                name: "AppStoreEvents");

            migrationBuilder.DropTable(
                name: "AppSubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_AppSubscriptions_PreviousExternalId",
                table: "AppSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_AppSubscriptions_Source_ExternalId",
                table: "AppSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_AppSubscriptions_UserId",
                table: "AppSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_AppSubscriptions_UserId_ExpiresAt",
                table: "AppSubscriptions");

            migrationBuilder.DropColumn(
                name: "AutoRenew",
                table: "AppSubscriptions");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "AppSubscriptions");

            migrationBuilder.DropColumn(
                name: "IsSandbox",
                table: "AppSubscriptions");

            migrationBuilder.DropColumn(
                name: "PreviousExternalId",
                table: "AppSubscriptions");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "AppSubscriptions");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "AppSubscriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AppSubscriptions");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "AppSubscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_AppSubscriptions_UserId",
                table: "AppSubscriptions",
                column: "UserId",
                unique: true);
        }
    }
}
